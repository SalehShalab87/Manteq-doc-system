using TMS.WebApi.Models;
using CMS.WebApi.Services;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.CustomProperties;
using DocumentFormat.OpenXml;
using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace TMS.WebApi.Services
{
    public interface IDocumentGenerationService
    {
        Task<DocumentGenerationResponse> GenerateDocumentAsync(DocumentGenerationRequest request);
        Task<byte[]> DownloadGeneratedDocumentAsync(Guid generationId);
        Task<string> GetGeneratedDocumentPathAsync(Guid generationId);
        Task<bool> CleanupExpiredDocumentsAsync();
    }

    public class DocumentGenerationService : IDocumentGenerationService
    {
        private readonly ITemplateService _templateService;
        private readonly IDocumentService _cmsDocumentService;
        private readonly ILogger<DocumentGenerationService> _logger;
        private readonly TmsSettings _tmsSettings;
        private readonly string _outputDirectory;
        private readonly string _libreOfficePath;
        private readonly Dictionary<Guid, GeneratedDocument> _generatedDocuments;
        private readonly Timer _cleanupTimer;

        public DocumentGenerationService(
            ITemplateService templateService,
            IDocumentService cmsDocumentService,
            ILogger<DocumentGenerationService> logger,
            IOptions<TmsSettings> tmsSettings)
        {
            _templateService = templateService;
            _cmsDocumentService = cmsDocumentService;
            _logger = logger;
            _tmsSettings = tmsSettings.Value;
            
            // Create output directory for generated documents
            _outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "GeneratedDocuments");
            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }

            // Find LibreOffice installation
            _libreOfficePath = FindLibreOfficePath();
            
            // In-memory tracking of generated documents (for temporary storage)
            _generatedDocuments = new Dictionary<Guid, GeneratedDocument>();

            // Setup cleanup timer using configuration
            var cleanupInterval = TimeSpan.FromMinutes(_tmsSettings.CleanupIntervalMinutes);
            _cleanupTimer = new Timer(async _ => await CleanupExpiredDocumentsAsync(), 
                null, cleanupInterval, cleanupInterval);
                
            _logger.LogInformation("📅 TMS Settings: Retention={RetentionHours}h, Cleanup every {CleanupMinutes}min, MaxSize={MaxSizeMB}MB", 
                _tmsSettings.DocumentRetentionHours, _tmsSettings.CleanupIntervalMinutes, _tmsSettings.MaxFileSizeMB);
        }

        public async Task<DocumentGenerationResponse> GenerateDocumentAsync(DocumentGenerationRequest request)
        {
            try
            {
                _logger.LogInformation("Starting document generation for template: {TemplateId}", request.TemplateId);

                // Get template information
                var template = await _templateService.GetTemplateAsync(request.TemplateId);
                if (template == null)
                {
                    throw new ArgumentException($"Template with ID '{request.TemplateId}' not found");
                }

                // Get the template file from CMS
                var templateFilePath = await _cmsDocumentService.GetDocumentFilePathAsync(template.CmsDocumentId);
                if (!File.Exists(templateFilePath))
                {
                    throw new FileNotFoundException($"Template file not found: {templateFilePath}");
                }

                _logger.LogInformation("Processing template: {TemplateName} with {PropertyCount} properties", 
                    template.Name, request.PropertyValues.Count);

                // Create unique identifiers for this generation
                var generationId = Guid.NewGuid();
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var sanitizedTemplateName = SanitizeFileName(template.Name);
                
                // Determine output file extension based on export format
                var outputExtension = GetOutputExtension(request.ExportFormat, templateFilePath);
                var outputFileName = $"{sanitizedTemplateName}_{timestamp}_{generationId:N}[0..8]{outputExtension}";
                var outputPath = Path.Combine(_outputDirectory, outputFileName);

                // Create working copy of the template
                var workingFilePath = await CreateWorkingCopyAsync(templateFilePath, generationId);
                
                try
                {
                    // Replace placeholders in the working copy
                    var processedCount = await ReplacePlaceholdersAsync(workingFilePath, request.PropertyValues);
                    _logger.LogInformation("Replaced {ProcessedCount} placeholders in template", processedCount);

                    // Convert to requested format
                    var finalPath = await ConvertToRequestedFormatAsync(workingFilePath, outputPath, request.ExportFormat);
                    
                    // Get file information
                    var fileInfo = new FileInfo(finalPath);
                    
                    // Create generated document record
                    var generatedDoc = new GeneratedDocument
                    {
                        Id = generationId,
                        FileName = Path.GetFileName(finalPath),
                        FilePath = finalPath,
                        FileSizeBytes = fileInfo.Length,
                        ExpiresAt = DateTime.UtcNow.AddHours(_tmsSettings.DocumentRetentionHours), // Use configurable retention
                        ExportFormat = request.ExportFormat,
                        SourceTemplateId = request.TemplateId,
                        GeneratedBy = request.GeneratedBy
                    };

                    // Track the generated document
                    _generatedDocuments[generationId] = generatedDoc;

                    _logger.LogInformation("Document generation completed successfully. File: {FileName}, Size: {FileSize} bytes", 
                        generatedDoc.FileName, generatedDoc.FileSizeBytes);

                    return new DocumentGenerationResponse
                    {
                        GenerationId = generationId,
                        Message = "Document generated successfully",
                        FileName = generatedDoc.FileName,
                        FileSizeBytes = generatedDoc.FileSizeBytes,
                        DownloadUrl = $"/api/templates/download/{generationId}",
                        ExpiresAt = generatedDoc.ExpiresAt,
                        ExportFormat = request.ExportFormat,
                        ProcessedPlaceholders = processedCount
                    };
                }
                finally
                {
                    // Clean up working copy
                    if (File.Exists(workingFilePath))
                    {
                        File.Delete(workingFilePath);
                        _logger.LogDebug("Cleaned up working file: {WorkingFilePath}", workingFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating document for template: {TemplateId}", request.TemplateId);
                throw;
            }
        }

        public async Task<byte[]> DownloadGeneratedDocumentAsync(Guid generationId)
        {
            if (!_generatedDocuments.TryGetValue(generationId, out var document))
            {
                throw new FileNotFoundException($"Generated document with ID '{generationId}' not found or has expired");
            }

            if (DateTime.UtcNow > document.ExpiresAt)
            {
                // Document has expired, clean it up
                await CleanupSingleDocumentAsync(generationId);
                throw new FileNotFoundException($"Generated document with ID '{generationId}' has expired");
            }

            if (!File.Exists(document.FilePath))
            {
                throw new FileNotFoundException($"Generated document file not found: {document.FilePath}");
            }

            _logger.LogInformation("Downloaded generated document: {FileName} ({FileSize} bytes)", 
                document.FileName, document.FileSizeBytes);

            return await File.ReadAllBytesAsync(document.FilePath);
        }

        public async Task<string> GetGeneratedDocumentPathAsync(Guid generationId)
        {
            if (!_generatedDocuments.TryGetValue(generationId, out var document))
            {
                throw new FileNotFoundException($"Generated document with ID '{generationId}' not found");
            }

            if (DateTime.UtcNow > document.ExpiresAt)
            {
                await CleanupSingleDocumentAsync(generationId);
                throw new FileNotFoundException($"Generated document with ID '{generationId}' has expired");
            }

            return document.FilePath;
        }

        public async Task<bool> CleanupExpiredDocumentsAsync()
        {
            try
            {
                var expiredIds = _generatedDocuments
                    .Where(kvp => DateTime.UtcNow > kvp.Value.ExpiresAt)
                    .Select(kvp => kvp.Key)
                    .ToList();

                var cleanedUp = 0;
                foreach (var expiredId in expiredIds)
                {
                    if (await CleanupSingleDocumentAsync(expiredId))
                    {
                        cleanedUp++;
                    }
                }

                if (cleanedUp > 0)
                {
                    _logger.LogInformation("Cleaned up {CleanedUpCount} expired generated documents", cleanedUp);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup of expired documents");
                return false;
            }
        }

        private async Task<string> CreateWorkingCopyAsync(string templateFilePath, Guid generationId)
        {
            var workingFileName = $"working_{generationId:N}_{Path.GetFileName(templateFilePath)}";
            var workingPath = Path.Combine(_outputDirectory, workingFileName);
            
            // Create working copy
            File.Copy(templateFilePath, workingPath, true);
            
            _logger.LogDebug("Created working copy: {WorkingPath}", workingPath);
            return workingPath;
        }

        private async Task<int> ReplacePlaceholdersAsync(string filePath, Dictionary<string, string> propertyValues)
        {
            var documentType = GetDocumentTypeFromPath(filePath);
            var processedCount = 0;

            try
            {
                switch (documentType)
                {
                    case Models.DocumentType.Word:
                        using (var doc = WordprocessingDocument.Open(filePath, true))
                        {
                            processedCount = await UpdateWordDocumentAsync(doc, propertyValues);
                            doc.MainDocumentPart?.Document?.Save();
                        }
                        break;

                    case Models.DocumentType.Excel:
                        using (var doc = SpreadsheetDocument.Open(filePath, true))
                        {
                            processedCount = await UpdateExcelDocumentAsync(doc, propertyValues);
                            doc.WorkbookPart?.Workbook?.Save();
                        }
                        break;

                    case Models.DocumentType.PowerPoint:
                        using (var doc = PresentationDocument.Open(filePath, true))
                        {
                            processedCount = await UpdatePowerPointDocumentAsync(doc, propertyValues);
                            doc.PresentationPart?.Presentation?.Save();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replacing placeholders in {FilePath}", filePath);
                throw;
            }

            return processedCount;
        }

        private async Task<int> UpdateWordDocumentAsync(WordprocessingDocument doc, Dictionary<string, string> propertyValues)
        {
            await UpdateCustomPropertiesAsync(doc, propertyValues);
            return await RefreshWordDocPropertyFieldsAsync(doc, propertyValues);
        }

        private async Task<int> UpdateExcelDocumentAsync(SpreadsheetDocument doc, Dictionary<string, string> propertyValues)
        {
            await UpdateCustomPropertiesAsync(doc, propertyValues);
            return propertyValues.Count; // Return count of processed properties
        }

        private async Task<int> UpdatePowerPointDocumentAsync(PresentationDocument doc, Dictionary<string, string> propertyValues)
        {
            await UpdateCustomPropertiesAsync(doc, propertyValues);
            return propertyValues.Count; // Return count of processed properties
        }

        private async Task UpdateCustomPropertiesAsync(OpenXmlPackage package, Dictionary<string, string> propertyValues)
        {
            await Task.Run(() => UpdateCustomProperties(package, propertyValues));
        }

        private void UpdateCustomProperties(OpenXmlPackage package, Dictionary<string, string> propertyValues)
        {
            try
            {
                CustomFilePropertiesPart? customPropertiesPart = null;

                // Get the custom properties part based on document type
                if (package is WordprocessingDocument wordDoc)
                {
                    if (wordDoc.CustomFilePropertiesPart == null)
                        wordDoc.AddCustomFilePropertiesPart();
                    customPropertiesPart = wordDoc.CustomFilePropertiesPart;
                }
                else if (package is SpreadsheetDocument excelDoc)
                {
                    if (excelDoc.CustomFilePropertiesPart == null)
                        excelDoc.AddCustomFilePropertiesPart();
                    customPropertiesPart = excelDoc.CustomFilePropertiesPart;
                }
                else if (package is PresentationDocument pptDoc)
                {
                    if (pptDoc.CustomFilePropertiesPart == null)
                        pptDoc.AddCustomFilePropertiesPart();
                    customPropertiesPart = pptDoc.CustomFilePropertiesPart;
                }

                if (customPropertiesPart == null) return;
                if (customPropertiesPart.Properties == null)
                {
                    customPropertiesPart.Properties = new DocumentFormat.OpenXml.CustomProperties.Properties();
                }

                var properties = customPropertiesPart.Properties;

                foreach (var kvp in propertyValues)
                {
                    // Find existing property
                    var existingProp = properties.Elements<CustomDocumentProperty>()
                        .FirstOrDefault(p => p.Name?.Value == kvp.Key);

                    if (existingProp != null)
                    {
                        // Update existing property
                        UpdatePropertyValue(existingProp, kvp.Value);
                    }
                    else
                    {
                        // Create new property
                        CreateNewCustomProperty(properties, kvp.Key, kvp.Value);
                    }
                }

                customPropertiesPart.Properties.Save();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not update custom properties: {Message}", ex.Message);
            }
        }

        private void UpdatePropertyValue(CustomDocumentProperty prop, string newValue)
        {
            // Remove existing value elements
            prop.RemoveAllChildren();

            // Add new value as string
            prop.AppendChild(new DocumentFormat.OpenXml.VariantTypes.VTLPWSTR(newValue));
        }

        private void CreateNewCustomProperty(DocumentFormat.OpenXml.CustomProperties.Properties properties, string name, string value)
        {
            // Get next property ID
            int pid = 2; // Start from 2 (1 is reserved)
            var existingProps = properties.Elements<CustomDocumentProperty>();
            if (existingProps.Any())
            {
                pid = existingProps.Max(p => p.PropertyId?.Value ?? 1) + 1;
            }

            var newProp = new CustomDocumentProperty()
            {
                FormatId = "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}",
                PropertyId = pid,
                Name = name
            };

            newProp.AppendChild(new DocumentFormat.OpenXml.VariantTypes.VTLPWSTR(value));
            properties.AppendChild(newProp);
        }

        private async Task<int> RefreshWordDocPropertyFieldsAsync(WordprocessingDocument doc, Dictionary<string, string> propertyValues)
        {
            return await Task.Run(() => RefreshWordDocPropertyFields(doc, propertyValues));
        }

        private int RefreshWordDocPropertyFields(WordprocessingDocument doc, Dictionary<string, string> propertyValues)
        {
            try
            {
                var containers = new List<OpenXmlElement>();
                
                if (doc.MainDocumentPart?.Document?.Body != null)
                    containers.Add(doc.MainDocumentPart.Document.Body);
                
                if (doc.MainDocumentPart != null)
                {
                    foreach (var headerPart in doc.MainDocumentPart.HeaderParts)
                        if (headerPart.Header != null) containers.Add(headerPart.Header);
                    
                    foreach (var footerPart in doc.MainDocumentPart.FooterParts)
                        if (footerPart.Footer != null) containers.Add(footerPart.Footer);
                }

                int totalFieldsUpdated = 0;
                
                foreach (var container in containers)
                {
                    totalFieldsUpdated += UpdateSimpleFields(container, propertyValues);
                    totalFieldsUpdated += UpdateComplexFields(container, propertyValues);
                }

                // Force Word to update fields when document opens
                ForceFieldUpdateOnOpen(doc);
                ClearFieldCaches(doc);

                _logger.LogDebug("Updated {FieldCount} DOCPROPERTY fields", totalFieldsUpdated);
                return totalFieldsUpdated;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not refresh DOCPROPERTY fields: {Message}", ex.Message);
                return 0;
            }
        }

        private int UpdateSimpleFields(OpenXmlElement container, Dictionary<string, string> propertyValues)
        {
            var updatedCount = 0;
            var simpleFields = container.Descendants<SimpleField>().ToList();
            
            foreach (var field in simpleFields)
            {
                var instruction = field.Instruction?.Value;
                if (string.IsNullOrEmpty(instruction)) continue;

                var match = System.Text.RegularExpressions.Regex.Match(instruction, @"\bDOCPROPERTY\s+(\S+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (!match.Success) continue;

                string propertyName = match.Groups[1].Value.Trim();
                if (propertyValues.TryGetValue(propertyName, out var newValue))
                {
                    var textElements = field.Elements<Text>().ToList();
                    foreach (var textElement in textElements)
                    {
                        textElement.Text = newValue;
                    }
                    updatedCount++;
                }
            }
            
            return updatedCount;
        }

        private int UpdateComplexFields(OpenXmlElement container, Dictionary<string, string> propertyValues)
        {
            // Complex field handling implementation (similar to your original code)
            // This is a simplified version - can be enhanced with your full logic
            return 0;
        }

        private void ForceFieldUpdateOnOpen(WordprocessingDocument doc)
        {
            try
            {
                var settingsPart = doc.MainDocumentPart?.DocumentSettingsPart;
                if (settingsPart == null)
                {
                    settingsPart = doc.MainDocumentPart?.AddNewPart<DocumentSettingsPart>();
                    if (settingsPart != null)
                    {
                        settingsPart.Settings = new Settings();
                    }
                }

                var settings = settingsPart?.Settings;
                if (settings == null) return;
                
                var existingUpdateFields = settings.Elements<UpdateFieldsOnOpen>().ToList();
                foreach (var element in existingUpdateFields)
                {
                    element.Remove();
                }

                settings.Append(new UpdateFieldsOnOpen() { Val = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not set auto-update fields setting: {Message}", ex.Message);
            }
        }

        private void ClearFieldCaches(WordprocessingDocument doc)
        {
            try
            {
                var containers = new List<OpenXmlElement>();
                
                if (doc.MainDocumentPart?.Document?.Body != null)
                    containers.Add(doc.MainDocumentPart.Document.Body);

                foreach (var container in containers)
                {
                    var simpleFields = container.Descendants<SimpleField>().ToList();
                    foreach (var field in simpleFields)
                    {
                        var instruction = field.Instruction?.Value;
                        if (!string.IsNullOrEmpty(instruction) && instruction.Contains("DOCPROPERTY"))
                        {
                            field.Dirty = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not clear field caches: {Message}", ex.Message);
            }
        }

        private async Task<string> ConvertToRequestedFormatAsync(string inputPath, string outputPath, ExportFormat exportFormat)
        {
            switch (exportFormat)
            {
                case ExportFormat.Html:
                    return await ConvertToHtmlAsync(inputPath, outputPath);
                case ExportFormat.EmailHtml:
                    return await ConvertToEmailHtmlAsync(inputPath, outputPath);
                case ExportFormat.Pdf:
                    return await ConvertToPdfAsync(inputPath, outputPath);
                case ExportFormat.Word:
                    if (Path.GetExtension(inputPath).ToLowerInvariant() != ".docx")
                        return await ConvertToWordAsync(inputPath, outputPath);
                    else
                        File.Copy(inputPath, outputPath, true);
                    return outputPath;
                case ExportFormat.Original:
                default:
                    File.Copy(inputPath, outputPath, true);
                    return outputPath;
            }
        }

        private async Task<string> ConvertToHtmlAsync(string inputPath, string outputPath)
        {
            if (!string.IsNullOrEmpty(_libreOfficePath))
            {
                var htmlPath = await ConvertUsingLibreOfficeAsync(inputPath, outputPath, "html");
                
                // Clean up LibreOffice field references in regular HTML too
                var htmlContent = File.ReadAllText(htmlPath);
                var cleanedHtml = CleanupLibreOfficeFields(htmlContent);
                File.WriteAllText(htmlPath, cleanedHtml);
                
                _logger.LogInformation("✅ Created HTML with cleaned up fields: {OutputPath}", htmlPath);
                return htmlPath;
            }
            
            // Fallback to basic conversion
            return await ConvertToHtmlFallbackAsync(inputPath, outputPath);
        }

        private async Task<string> ConvertToEmailHtmlAsync(string inputPath, string outputPath)
        {
            if (!string.IsNullOrEmpty(_libreOfficePath))
            {
                // First convert to regular HTML using LibreOffice
                var tempHtmlPath = Path.ChangeExtension(inputPath, ".temp.html");
                var libreOfficeHtmlPath = await ConvertUsingLibreOfficeAsync(inputPath, tempHtmlPath, "html");
                
                try
                {
                    // LibreOffice may create the HTML file with a different name pattern
                    var actualTempHtmlPath = libreOfficeHtmlPath;
                    if (!File.Exists(actualTempHtmlPath))
                    {
                        // Try to find the actual HTML file created by LibreOffice
                        var inputFileNameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);
                        var tempHtmlDir = Path.GetDirectoryName(tempHtmlPath);
                        var possibleHtmlFile = Path.Combine(tempHtmlDir!, $"{inputFileNameWithoutExt}.html");
                        
                        if (File.Exists(possibleHtmlFile))
                        {
                            actualTempHtmlPath = possibleHtmlFile;
                        }
                    }
                    
                    if (File.Exists(actualTempHtmlPath))
                    {
                        _logger.LogInformation("📄 Found LibreOffice HTML output at: {HtmlPath}", actualTempHtmlPath);
                        
                        // Also look for image files that LibreOffice created
                        var htmlDirectory = Path.GetDirectoryName(actualTempHtmlPath);
                        var htmlFileNamePattern = Path.GetFileNameWithoutExtension(actualTempHtmlPath);
                        
                        // List all potential image files created by LibreOffice
                        if (htmlDirectory != null)
                        {
                            var imageFiles = Directory.GetFiles(htmlDirectory, "*.gif")
                                .Concat(Directory.GetFiles(htmlDirectory, "*.png"))
                                .Concat(Directory.GetFiles(htmlDirectory, "*.jpg"))
                                .Concat(Directory.GetFiles(htmlDirectory, "*.jpeg"))
                                .Where(f => Path.GetFileName(f).Contains(htmlFileNamePattern) || 
                                           Path.GetFileName(f).Contains("working_"))
                                .ToList();
                                
                            _logger.LogInformation("📷 Found {ImageCount} image files from LibreOffice: {ImageFiles}", 
                                imageFiles.Count, string.Join(", ", imageFiles.Select(Path.GetFileName)));
                        }
                        
                        // Convert to email-friendly HTML with embedded images
                        var emailHtmlPath = await ConvertToEmailFriendlyHtmlAsync(actualTempHtmlPath, outputPath);
                        
                        return emailHtmlPath;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ LibreOffice HTML output not found at expected location: {ExpectedPath}", actualTempHtmlPath);
                        return await ConvertToHtmlFallbackAsync(inputPath, outputPath);
                    }
                }
                finally
                {
                    // Clean up temporary files - but do it after we've processed them
                    await CleanupLibreOfficeFilesAsync(inputPath, tempHtmlPath);
                }
            }
            
            return await ConvertToHtmlFallbackAsync(inputPath, outputPath);
        }

        private async Task<string> ConvertToEmailFriendlyHtmlAsync(string htmlPath, string outputPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var htmlContent = File.ReadAllText(htmlPath);
                    var htmlDirectory = Path.GetDirectoryName(htmlPath);
                    
                    // Convert external image references to base64 embedded images
                    var emailHtml = ConvertImagesToBase64(htmlContent, htmlDirectory);
                    
                    // Apply email-friendly styling
                    emailHtml = ApplyEmailFriendlyStyles(emailHtml);
                    
                    // Clean up LibreOffice field references that weren't processed
                    emailHtml = CleanupLibreOfficeFields(emailHtml);
                    
                    File.WriteAllText(outputPath, emailHtml);
                    
                    _logger.LogInformation("✅ Created email-friendly HTML: {OutputPath}", outputPath);
                    return outputPath;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating email-friendly HTML");
                    // Fallback to copying original file
                    File.Copy(htmlPath, outputPath, true);
                    return outputPath;
                }
            });
        }

        private string ConvertImagesToBase64(string htmlContent, string htmlDirectory)
        {
            try
            {
                _logger.LogInformation("🔍 Converting external images to base64 for email compatibility...");
                
                // Find all image references in the HTML
                var imgPattern = @"<img[^>]+src\s*=\s*[""']([^""']+)[""'][^>]*>";
                var matches = System.Text.RegularExpressions.Regex.Matches(htmlContent, imgPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                var embeddedCount = 0;
                
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var imgTag = match.Value;
                    var imgSrc = match.Groups[1].Value;
                    
                    _logger.LogDebug("📷 Found image reference: {ImageSrc}", imgSrc);
                    
                    // Skip if already base64 or external URL
                    if (imgSrc.StartsWith("data:") || imgSrc.StartsWith("http"))
                    {
                        _logger.LogDebug("⏩ Skipping external/data URI: {ImageSrc}", imgSrc);
                        continue;
                    }
                    
                    // Decode URL-encoded image source (LibreOffice creates URL-encoded filenames)
                    var decodedImgSrc = Uri.UnescapeDataString(imgSrc);
                    
                    // Try multiple strategies to find the image file
                    string? imagePath = null;
                    var searchDirectory = htmlDirectory ?? _outputDirectory;
                    
                    // Strategy 1: Try the exact filename as referenced
                    if (Path.IsPathRooted(decodedImgSrc))
                    {
                        imagePath = File.Exists(decodedImgSrc) ? decodedImgSrc : null;
                    }
                    else
                    {
                        var candidatePath = Path.Combine(searchDirectory, decodedImgSrc);
                        imagePath = File.Exists(candidatePath) ? candidatePath : null;
                    }
                    
                    // Strategy 2: Try with original encoded name
                    if (imagePath == null && imgSrc != decodedImgSrc)
                    {
                        var encodedPath = Path.Combine(searchDirectory, imgSrc);
                        imagePath = File.Exists(encodedPath) ? encodedPath : null;
                    }
                    
                    // Strategy 3: Search by pattern in the directory
                    if (imagePath == null)
                    {
                        var fileName = Path.GetFileName(decodedImgSrc);
                        var fileNamePattern = fileName.Split('_')[^1]; // Get the last part after underscore
                        
                        var searchPattern = $"*{fileNamePattern}*";
                        var matchingFiles = Directory.GetFiles(searchDirectory, searchPattern, SearchOption.TopDirectoryOnly);
                        
                        imagePath = matchingFiles.FirstOrDefault();
                        
                        if (imagePath != null)
                        {
                            _logger.LogDebug("📁 Found image by pattern search: {ImagePath}", imagePath);
                        }
                    }
                    
                    // Strategy 4: Try to find any gif/png/jpg file with similar name parts
                    if (imagePath == null)
                    {
                        var extensions = new[] { "*.gif", "*.png", "*.jpg", "*.jpeg" };
                        var nameparts = Path.GetFileNameWithoutExtension(decodedImgSrc).Split('_');
                        
                        foreach (var ext in extensions)
                        {
                            var allFiles = Directory.GetFiles(searchDirectory, ext);
                            foreach (var file in allFiles)
                            {
                                var fileBaseName = Path.GetFileNameWithoutExtension(file);
                                if (nameparts.Any(part => fileBaseName.Contains(part) && part.Length > 5))
                                {
                                    imagePath = file;
                                    _logger.LogDebug("🎯 Found image by name matching: {ImagePath}", imagePath);
                                    break;
                                }
                            }
                            if (imagePath != null) break;
                        }
                    }
                    
                    _logger.LogDebug("🔍 Final image path resolved to: {ImagePath}", imagePath ?? "NOT FOUND");
                    
                    if (imagePath != null && File.Exists(imagePath))
                    {
                        try
                        {
                            var imageBytes = File.ReadAllBytes(imagePath);
                            var mimeType = GetMimeTypeFromExtension(Path.GetExtension(imagePath));
                            var base64Image = Convert.ToBase64String(imageBytes);
                            var dataUri = $"data:{mimeType};base64,{base64Image}";
                            
                            // Replace the src attribute with more robust pattern matching
                            var updatedImgTag = System.Text.RegularExpressions.Regex.Replace(
                                imgTag, 
                                @"src\s*=\s*[""']([^""']+)[""']", 
                                $"src=\"{dataUri}\"", 
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            
                            htmlContent = htmlContent.Replace(imgTag, updatedImgTag);
                            embeddedCount++;
                            
                            _logger.LogInformation("✅ Embedded image ({FileSize} KB): {ImagePath}", 
                                imageBytes.Length / 1024, Path.GetFileName(imagePath));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "❌ Failed to embed image: {ImagePath}", imagePath);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Image file not found after all search strategies: {ImageSrc} -> {DecodedSrc}", 
                            imgSrc, decodedImgSrc);
                            
                        // List available files for debugging
                        if (Directory.Exists(searchDirectory))
                        {
                            var availableImages = Directory.GetFiles(searchDirectory, "*.*")
                                .Where(f => new[] { ".gif", ".png", ".jpg", ".jpeg" }.Contains(Path.GetExtension(f).ToLower()))
                                .Select(Path.GetFileName)
                                .ToList();
                                
                            if (availableImages.Count > 0)
                            {
                                _logger.LogDebug("📂 Available image files: {AvailableFiles}", string.Join(", ", availableImages));
                            }
                        }
                    }
                }
                
                _logger.LogInformation("🎯 Successfully embedded {EmbeddedCount} images as base64 data URIs", embeddedCount);
                return htmlContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error converting images to base64");
                return htmlContent;
            }
        }

        private string ApplyEmailFriendlyStyles(string htmlContent)
        {
            try
            {
                _logger.LogInformation("📧 Applying email-friendly styling...");
                
                // Apply inline styles for better email compatibility
                var emailFriendlyHtml = htmlContent;
                
                // Add email-friendly meta tags if not present
                if (!htmlContent.Contains("viewport"))
                {
                    emailFriendlyHtml = emailFriendlyHtml.Replace("<head>", 
                        "<head>\n\t<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
                }
                
                // Ensure UTF-8 encoding
                if (!htmlContent.Contains("charset"))
                {
                    emailFriendlyHtml = emailFriendlyHtml.Replace("<head>", 
                        "<head>\n\t<meta charset=\"UTF-8\">");
                }
                
                // Remove or inline styles that don't work well in email clients
                // Replace @page rules which don't work in email
                emailFriendlyHtml = System.Text.RegularExpressions.Regex.Replace(
                    emailFriendlyHtml, 
                    @"@page\s*{[^}]*}", 
                    "", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                // Convert some common problematic styles to email-safe versions
                emailFriendlyHtml = emailFriendlyHtml
                    .Replace("margin-left: 1.25in;", "margin-left: 120px;")
                    .Replace("margin-right: 1.25in;", "margin-right: 120px;")
                    .Replace("margin-top: 1in;", "margin-top: 96px;")
                    .Replace("margin-bottom: 1in;", "margin-bottom: 96px;");
                
                // Add email-safe table styling
                emailFriendlyHtml = System.Text.RegularExpressions.Regex.Replace(
                    emailFriendlyHtml,
                    @"<table(?![^>]*style)",
                    "<table style=\"border-collapse: collapse; width: 100%; max-width: 600px;\"",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                // Ensure body has email-safe styling
                if (!System.Text.RegularExpressions.Regex.IsMatch(emailFriendlyHtml, @"<body[^>]*style", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    emailFriendlyHtml = System.Text.RegularExpressions.Regex.Replace(
                        emailFriendlyHtml,
                        @"<body([^>]*)>",
                        "<body$1 style=\"font-family: Arial, Helvetica, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 20px; background-color: #ffffff;\">",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                
                _logger.LogInformation("✅ Applied email-friendly styling");
                return emailFriendlyHtml;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error applying email-friendly styles");
                return htmlContent;
            }
        }

        private string CleanupLibreOfficeFields(string htmlContent)
        {
            try
            {
                _logger.LogInformation("🧹 Cleaning up LibreOffice field references...");
                
                var cleanHtml = htmlContent;
                var cleanupCount = 0;
                
                // Remove sdfield tags and replace with their content
                var sdfieldPattern = @"<sdfield[^>]*>(.*?)</sdfield>";
                var sdfieldMatches = System.Text.RegularExpressions.Regex.Matches(cleanHtml, sdfieldPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                cleanupCount += sdfieldMatches.Count;
                cleanHtml = System.Text.RegularExpressions.Regex.Replace(cleanHtml, sdfieldPattern, "$1", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                // Remove empty sdfield tags
                var emptySdfieldPattern = @"<sdfield[^>]*\s*/>";
                var emptySdfieldMatches = System.Text.RegularExpressions.Regex.Matches(cleanHtml, emptySdfieldPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                cleanupCount += emptySdfieldMatches.Count;
                cleanHtml = System.Text.RegularExpressions.Regex.Replace(cleanHtml, emptySdfieldPattern, "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                // Remove other LibreOffice-specific field references
                var fieldPatterns = new[]
                {
                    @"<field[^>]*>(.*?)</field>",
                    @"<field[^>]*\s*/>",
                    @"<!--\s*FIELD\s+[^>]+-->",
                    @"<!--\s*DOCPROPERTY\s+[^>]+-->"
                };
                
                foreach (var pattern in fieldPatterns)
                {
                    var matches = System.Text.RegularExpressions.Regex.Matches(cleanHtml, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    cleanupCount += matches.Count;
                    cleanHtml = System.Text.RegularExpressions.Regex.Replace(cleanHtml, pattern, "$1", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                
                // Remove LibreOffice meta generator tags
                cleanHtml = System.Text.RegularExpressions.Regex.Replace(
                    cleanHtml, 
                    @"<meta[^>]*generator[^>]*LibreOffice[^>]*>", 
                    "", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                // Clean up highlighted/background color spans that might contain unprocessed placeholders
                cleanHtml = System.Text.RegularExpressions.Regex.Replace(
                    cleanHtml,
                    @"<span[^>]*background:\s*#c0c0c0[^>]*>(.*?)</span>",
                    "$1",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (cleanupCount > 0)
                {
                    _logger.LogInformation("✅ Cleaned up {CleanupCount} LibreOffice field references", cleanupCount);
                }
                
                return cleanHtml;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error cleaning up LibreOffice fields");
                return htmlContent;
            }
        }

        private async Task CleanupLibreOfficeFilesAsync(string originalInputPath, string tempHtmlPath)
        {
            await Task.Run(() =>
            {
                try
                {
                    var inputFileNameWithoutExt = Path.GetFileNameWithoutExtension(originalInputPath);
                    var htmlDirectory = Path.GetDirectoryName(tempHtmlPath) ?? _outputDirectory;
                    
                    // Clean up the temporary HTML file
                    if (File.Exists(tempHtmlPath))
                    {
                        File.Delete(tempHtmlPath);
                        _logger.LogDebug("🧹 Cleaned up temporary HTML: {TempHtmlPath}", tempHtmlPath);
                    }
                    
                    // Clean up HTML file with actual name pattern
                    var actualHtmlFile = Path.Combine(htmlDirectory, $"{inputFileNameWithoutExt}.html");
                    if (File.Exists(actualHtmlFile))
                    {
                        File.Delete(actualHtmlFile);
                        _logger.LogDebug("🧹 Cleaned up LibreOffice HTML: {HtmlFile}", actualHtmlFile);
                    }
                    
                    // Clean up associated image files created by LibreOffice
                    var imageFiles = Directory.GetFiles(htmlDirectory, "*.gif")
                        .Concat(Directory.GetFiles(htmlDirectory, "*.png"))
                        .Concat(Directory.GetFiles(htmlDirectory, "*.jpg"))
                        .Concat(Directory.GetFiles(htmlDirectory, "*.jpeg"))
                        .Where(f => Path.GetFileName(f).Contains(inputFileNameWithoutExt) || 
                                   Path.GetFileName(f).Contains("working_"))
                        .ToList();
                    
                    foreach (var imageFile in imageFiles)
                    {
                        if (File.Exists(imageFile))
                        {
                            File.Delete(imageFile);
                            _logger.LogDebug("🧹 Cleaned up LibreOffice image: {ImageFile}", Path.GetFileName(imageFile));
                        }
                    }
                    
                    if (imageFiles.Count > 0)
                    {
                        _logger.LogInformation("🧹 Cleaned up {ImageCount} LibreOffice image files", imageFiles.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Error during LibreOffice cleanup");
                }
            });
        }

        private string GetMimeTypeFromExtension(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                _ => "image/png" // Default fallback
            };
        }

        private async Task<string> ConvertToPdfAsync(string inputPath, string outputPath)
        {
            if (!string.IsNullOrEmpty(_libreOfficePath))
            {
                return await ConvertUsingLibreOfficeAsync(inputPath, outputPath, "pdf");
            }
            
            throw new NotSupportedException("LibreOffice not found. PDF conversion is not available.");
        }

        private async Task<string> ConvertToWordAsync(string inputPath, string outputPath)
        {
            if (!string.IsNullOrEmpty(_libreOfficePath))
            {
                return await ConvertUsingLibreOfficeAsync(inputPath, outputPath, "docx");
            }
            
            throw new NotSupportedException("LibreOffice not found. Word conversion is not available.");
        }

        private async Task<string> ConvertUsingLibreOfficeAsync(string inputPath, string outputPath, string format)
        {
            return await Task.Run(() => ConvertUsingLibreOffice(inputPath, outputPath, format));
        }

        private string ConvertUsingLibreOffice(string inputPath, string outputPath, string format)
        {
            var outputDir = Path.GetDirectoryName(outputPath);
            var startInfo = new ProcessStartInfo
            {
                FileName = _libreOfficePath,
                Arguments = $"--headless --convert-to {format} --outdir \"{outputDir}\" \"{inputPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                    throw new InvalidOperationException("Failed to start LibreOffice process");

                if (!process.WaitForExit(_tmsSettings.LibreOfficeTimeout))
                {
                    process.Kill();
                    throw new TimeoutException($"LibreOffice conversion timed out after {_tmsSettings.LibreOfficeTimeout}ms");
                }

                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    throw new InvalidOperationException($"LibreOffice conversion failed: {error}");
                }
            }

            // LibreOffice may create file with different name
            var expectedFile = Path.Combine(outputDir!, $"{Path.GetFileNameWithoutExtension(inputPath)}.{format}");
            if (File.Exists(expectedFile) && expectedFile != outputPath)
            {
                File.Move(expectedFile, outputPath);
            }

            return outputPath;
        }

        private async Task<string> ConvertToHtmlFallbackAsync(string inputPath, string outputPath)
        {
            return await Task.Run(() => ConvertToHtmlFallback(inputPath, outputPath));
        }

        private string ConvertToHtmlFallback(string inputPath, string outputPath)
        {
            // Basic HTML conversion fallback
            var html = $"<html><body><h1>Document Conversion</h1><p>Original file: {Path.GetFileName(inputPath)}</p></body></html>";
            File.WriteAllText(outputPath, html);
            return outputPath;
        }

        private async Task<bool> CleanupSingleDocumentAsync(Guid generationId)
        {
            try
            {
                if (_generatedDocuments.TryGetValue(generationId, out var document))
                {
                    if (File.Exists(document.FilePath))
                    {
                        File.Delete(document.FilePath);
                        _logger.LogDebug("Deleted expired file: {FilePath}", document.FilePath);
                    }
                    
                    _generatedDocuments.Remove(generationId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up document: {GenerationId}", generationId);
                return false;
            }
        }

        private string FindLibreOfficePath()
        {
            string[] possiblePaths = {
                @"C:\Program Files\LibreOffice\program\soffice.exe",
                @"C:\Program Files (x86)\LibreOffice\program\soffice.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\LibreOffice\program\soffice.exe")
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    _logger.LogInformation("Found LibreOffice at: {Path}", path);
                    return path;
                }
            }

            _logger.LogWarning("LibreOffice not found. Some export formats will not be available.");
            return string.Empty;
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        }

        private static string GetOutputExtension(ExportFormat exportFormat, string templatePath)
        {
            return exportFormat switch
            {
                ExportFormat.Html or ExportFormat.EmailHtml => ".html",
                ExportFormat.Pdf => ".pdf",
                ExportFormat.Word => ".docx",
                ExportFormat.Original => Path.GetExtension(templatePath),
                _ => Path.GetExtension(templatePath)
            };
        }

        private static Models.DocumentType GetDocumentTypeFromPath(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".docx" => Models.DocumentType.Word,
                ".xlsx" => Models.DocumentType.Excel,
                ".pptx" => Models.DocumentType.PowerPoint,
                _ => Models.DocumentType.Word
            };
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }
    }
}
