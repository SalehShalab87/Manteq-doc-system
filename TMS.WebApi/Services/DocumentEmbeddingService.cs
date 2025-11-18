using TMS.WebApi.Models;
using TMS.WebApi.HttpClients;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace TMS.WebApi.Services
{
    public interface IDocumentEmbeddingService
    {
        Task<DocumentEmbeddingResponse> GenerateDocumentWithEmbeddingAsync(DocumentEmbeddingRequest request);
        Task<byte[]> DownloadGeneratedDocumentAsync(Guid generationId);
    }

    public class DocumentEmbeddingService : IDocumentEmbeddingService
    {
        private readonly ITemplateService _templateService;
        private readonly IDocumentGenerationService _documentGenerationService;
        private readonly ICmsApiClient _cmsApiClient;
        private readonly ILogger<DocumentEmbeddingService> _logger;
        private readonly TmsSettings _tmsSettings;
        private readonly string _outputDirectory;
        private readonly string _tempDirectory;
        private readonly Dictionary<Guid, GeneratedDocument> _generatedDocuments;

        public DocumentEmbeddingService(
            ITemplateService templateService,
            IDocumentGenerationService documentGenerationService,
            ICmsApiClient cmsApiClient,
            ILogger<DocumentEmbeddingService> logger,
            IOptions<TmsSettings> tmsSettings)
        {
            _templateService = templateService;
            _documentGenerationService = documentGenerationService;
            _cmsApiClient = cmsApiClient;
            _logger = logger;
            _tmsSettings = tmsSettings.Value;
            
            // Use shared storage path from configuration, fallback to current directory
            var sharedStoragePath = _tmsSettings.SharedStoragePath ?? 
                                  Path.Combine(Directory.GetCurrentDirectory(), "GeneratedDocuments");
            _outputDirectory = sharedStoragePath;
            
            // Use shared temp path from configuration, fallback to current directory
            var tempPath = _tmsSettings.TempUploadPath ?? 
                          Path.Combine(Directory.GetCurrentDirectory(), "TempEmbedding");
            _tempDirectory = tempPath;
            
            // Ensure directories exist
            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
                _logger.LogInformation("Created shared output directory: {OutputDirectory}", _outputDirectory);
            }
            
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
                _logger.LogInformation("Created shared temp directory: {TempDirectory}", _tempDirectory);
            }

            // In-memory tracking of generated documents (for temporary storage)
            _generatedDocuments = new Dictionary<Guid, GeneratedDocument>();
            
            _logger.LogInformation("📁 DocumentEmbeddingService initialized with shared storage: Output={OutputDirectory}, Temp={TempDirectory}", 
                _outputDirectory, _tempDirectory);
        }

        public async Task<DocumentEmbeddingResponse> GenerateDocumentWithEmbeddingAsync(DocumentEmbeddingRequest request)
        {
            try
            {
                _logger.LogInformation("🔄 Starting document embedding generation for main template: {MainTemplateId}", request.MainTemplateId);

                // Get main template information
                var mainTemplate = await _templateService.GetTemplateAsync(request.MainTemplateId);
                if (mainTemplate == null)
                {
                    throw new ArgumentException($"Main template with ID '{request.MainTemplateId}' not found");
                }

                // Validate that main template is Word document (embedding only works with Word)
                var mainTemplateFilePath = await _cmsApiClient.GetDocumentFilePathAsync(mainTemplate.CmsDocumentId);
                var documentType = GetDocumentTypeFromPath(mainTemplateFilePath);
                
                if (documentType != Models.DocumentType.Word)
                {
                    throw new ArgumentException("Document embedding is only supported for Word documents");
                }

                _logger.LogInformation("📋 Main template: {TemplateName} with {EmbeddingCount} embeddings", 
                    mainTemplate.Name, request.Embeddings.Count);

                // Create unique identifiers for this generation
                var generationId = Guid.NewGuid();
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var sanitizedTemplateName = SanitizeFileName(mainTemplate.Name);

                // Create main document with all placeholders filled except embed placeholders
                string workingDocPath = await CreateMainDocumentForEmbeddingAsync(mainTemplate, request, generationId);
                var embeddingResults = new List<string>();
                int processedEmbeddings = 0;

                try
                {
                    // Process each embedding
                    foreach (var embedding in request.Embeddings)
                    {
                        try
                        {
                            var result = await ProcessSingleEmbeddingAsync(workingDocPath, embedding);
                            embeddingResults.Add(result);
                            if (result.StartsWith("✅"))
                            {
                                processedEmbeddings++;
                            }
                        }
                        catch (Exception ex)
                        {
                            var errorMsg = $"❌ Failed to process embedding '{embedding.EmbedPlaceholder}': {ex.Message}";
                            embeddingResults.Add(errorMsg);
                            _logger.LogWarning(ex, "Failed to process embedding: {EmbedPlaceholder}", embedding.EmbedPlaceholder);
                        }
                    }

                    // Export to final format
                    var outputExtension = GetOutputExtension(request.ExportFormat, workingDocPath);
                    var outputFileName = $"{sanitizedTemplateName}_with_embeddings_{timestamp}_{generationId:N}[0..8]{outputExtension}";
                    var finalPath = await ConvertToFinalFormatAsync(workingDocPath, request.ExportFormat, outputFileName);

                    // Get file information
                    var fileInfo = new FileInfo(finalPath);

                    // Create generated document record
                    var generatedDoc = new GeneratedDocument
                    {
                        Id = generationId,
                        FileName = Path.GetFileName(finalPath),
                        FilePath = finalPath,
                        FileSizeBytes = fileInfo.Length,
                        ExpiresAt = DateTime.UtcNow.AddHours(_tmsSettings.DocumentRetentionHours), // Use TMS settings
                        ExportFormat = request.ExportFormat,
                        SourceTemplateId = request.MainTemplateId,
                        GeneratedBy = request.GeneratedBy
                    };

                    // Track the generated document
                    _generatedDocuments[generationId] = generatedDoc;

                    _logger.LogInformation("✅ Document embedding generation completed successfully. File: {FileName}, Embeddings: {ProcessedEmbeddings}/{TotalEmbeddings}", 
                        generatedDoc.FileName, processedEmbeddings, request.Embeddings.Count);

                    return new DocumentEmbeddingResponse
                    {
                        GenerationId = generationId,
                        Message = $"Document with embeddings generated successfully: {processedEmbeddings}/{request.Embeddings.Count} embeddings processed",
                        FileName = generatedDoc.FileName,
                        FileSizeBytes = generatedDoc.FileSizeBytes,
                        DownloadUrl = $"/api/templates/download/{generationId}",
                        ExpiresAt = generatedDoc.ExpiresAt,
                        ExportFormat = request.ExportFormat,
                        ProcessedEmbeddings = processedEmbeddings,
                        ProcessedMainPlaceholders = request.MainTemplateValues.Count,
                        EmbeddingResults = embeddingResults
                    };
                }
                finally
                {
                    // Clean up working copy
                    if (File.Exists(workingDocPath))
                    {
                        File.Delete(workingDocPath);
                        _logger.LogDebug("🧹 Cleaned up working document: {WorkingDocPath}", workingDocPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating document with embeddings for template: {MainTemplateId}", request.MainTemplateId);
                throw;
            }
        }

        private async Task<string> CreateMainDocumentForEmbeddingAsync(RetrieveTemplateResponse mainTemplate, DocumentEmbeddingRequest request, Guid generationId)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var workingFileName = $"working_main_{timestamp}_{generationId:N}.docx";
            var workingPath = Path.Combine(_tempDirectory, workingFileName);

            // Get the main template file from CMS via HTTP API
            var mainTemplateFilePath = await _cmsApiClient.GetDocumentFilePathAsync(mainTemplate.CmsDocumentId);
            
            // Copy main template to working location
            File.Copy(mainTemplateFilePath, workingPath, true);
            _logger.LogDebug("📝 Created working main document: {WorkingFileName}", workingFileName);

            // Get embed placeholders from all embeddings
            var embedPlaceholders = request.Embeddings.Select(e => e.EmbedPlaceholder).ToHashSet();

            // Replace only non-embed placeholders
            var nonEmbedValues = request.MainTemplateValues
                .Where(kvp => !embedPlaceholders.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            if (nonEmbedValues.Any())
            {
                await ReplacePlaceholdersInDocumentAsync(workingPath, nonEmbedValues);
                _logger.LogInformation("✅ Replaced {NonEmbedCount} main template placeholders", nonEmbedValues.Count);
            }

            return workingPath;
        }

        private async Task<string> ProcessSingleEmbeddingAsync(string mainDocPath, EmbedInfo embedding)
        {
            var embedTemplate = await _templateService.GetTemplateAsync(embedding.EmbedTemplateId);
            if (embedTemplate == null)
            {
                return $"❌ Embed template with ID '{embedding.EmbedTemplateId}' not found. Skipping.";
            }

            // Get embed template file path via HTTP API
            var embedTemplateFilePath = await _cmsApiClient.GetDocumentFilePathAsync(embedTemplate.CmsDocumentId);
            var embedDocumentType = GetDocumentTypeFromPath(embedTemplateFilePath);

            if (embedDocumentType != Models.DocumentType.Word)
            {
                return $"⚠️ Embed template '{embedTemplate.Name}' is not a Word document. Skipping.";
            }

            _logger.LogInformation("🔗 Processing embedding: {EmbedTemplateName} → {EmbedPlaceholder}", embedTemplate.Name, embedding.EmbedPlaceholder);

            // Create processed embed document
            string embedDocPath = await CreateProcessedEmbedDocumentAsync(embedTemplate, embedding.EmbedTemplateValues);

            try
            {
                // Embed the document at the specified placeholder
                await EmbedWordDocumentAsync(mainDocPath, embedDocPath, embedding.EmbedPlaceholder);
                return $"✅ Successfully embedded '{embedTemplate.Name}' at '{embedding.EmbedPlaceholder}'";
            }
            catch (Exception ex)
            {
                return $"❌ Failed to embed '{embedTemplate.Name}': {ex.Message}";
            }
            finally
            {
                // Clean up temporary embed document
                await CleanupTempFileAsync(embedDocPath, $"embed document '{embedTemplate.Name}'");
            }
        }

        private async Task<string> CreateProcessedEmbedDocumentAsync(RetrieveTemplateResponse embedTemplate, Dictionary<string, string> embedValues)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var random = Guid.NewGuid().ToString("N")[..8];
            var tempEmbedFileName = $"embed_temp_{timestamp}_{random}.docx";
            var tempEmbedPath = Path.Combine(_tempDirectory, tempEmbedFileName);

            // Get the embed template file from CMS and copy
            var embedTemplateFilePath = await _cmsApiClient.GetDocumentFilePathAsync(embedTemplate.CmsDocumentId);
            File.Copy(embedTemplateFilePath, tempEmbedPath, true);

            // Replace placeholders in embed document
            if (embedValues.Any())
            {
                await ReplacePlaceholdersInDocumentAsync(tempEmbedPath, embedValues);
                _logger.LogDebug("   ✓ Processed {EmbedValueCount} placeholders in embed template", embedValues.Count);
            }

            return tempEmbedPath;
        }

        private async Task EmbedWordDocumentAsync(string mainDocPath, string embedDocPath, string embedPlaceholder)
        {
            await Task.Run(() => EmbedWordDocument(mainDocPath, embedDocPath, embedPlaceholder));
        }

        private void EmbedWordDocument(string mainDocPath, string embedDocPath, string embedPlaceholder)
        {
            using (var mainDoc = WordprocessingDocument.Open(mainDocPath, true))
            using (var embedDoc = WordprocessingDocument.Open(embedDocPath, false))
            {
                if (mainDoc.MainDocumentPart?.Document?.Body == null || 
                    embedDoc.MainDocumentPart?.Document?.Body == null)
                {
                    throw new InvalidOperationException("Document structure is invalid");
                }

                // Find placeholder in main document
                var placeholderElement = FindPlaceholderInWordDocument(mainDoc, embedPlaceholder);
                if (placeholderElement == null)
                {
                    _logger.LogWarning("   ⚠️ Placeholder '{EmbedPlaceholder}' not found in main document", embedPlaceholder);
                    return;
                }

                _logger.LogDebug("   📍 Found placeholder '{EmbedPlaceholder}' in main document", embedPlaceholder);

                // Import styles from embed document to avoid style conflicts
                ImportStyles(mainDoc, embedDoc);

                // Import numbering definitions if present
                ImportNumberingDefinitions(mainDoc, embedDoc);

                // Import images and media
                ImportImages(mainDoc, embedDoc);

                // Clone and import all content from embed document
                var sourceBody = embedDoc.MainDocumentPart.Document.Body;
                var importedElements = new List<OpenXmlElement>();

                foreach (var element in sourceBody.Elements())
                {
                    var clonedElement = element.CloneNode(true);
                    importedElements.Add(clonedElement);
                }

                // Insert imported content at placeholder location
                var parentElement = placeholderElement.Parent;
                if (parentElement != null)
                {
                    // Insert all imported elements before the placeholder
                    foreach (var element in importedElements)
                    {
                        parentElement.InsertBefore(element, placeholderElement);
                    }
                    
                    // Remove the placeholder
                    placeholderElement.Remove();
                    _logger.LogDebug("   ✓ Inserted {ElementCount} elements from embed document", importedElements.Count);
                }

                mainDoc.MainDocumentPart.Document.Save();
            }
        }

        private async Task ReplacePlaceholdersInDocumentAsync(string filePath, Dictionary<string, string> propertyValues)
        {
            await Task.Run(() => ReplacePlaceholdersInDocument(filePath, propertyValues));
        }

        private void ReplacePlaceholdersInDocument(string filePath, Dictionary<string, string> propertyValues)
        {
            try
            {
                using (var doc = WordprocessingDocument.Open(filePath, true))
                {
                    // Update custom properties
                    UpdateCustomPropertiesInDocument(doc, propertyValues);
                    
                    // Refresh document property fields
                    RefreshWordDocPropertyFields(doc, propertyValues);
                    
                    doc.MainDocumentPart?.Document?.Save();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not update document properties in {FilePath}: {Message}", filePath, ex.Message);
            }
        }

        private async Task<string> ConvertToFinalFormatAsync(string workingDocPath, ExportFormat exportFormat, string outputFileName)
        {
            var outputPath = Path.Combine(_outputDirectory, outputFileName);

            switch (exportFormat)
            {
                case ExportFormat.Original:
                case ExportFormat.Word:
                    // Simply copy to final location with proper name for Word/Original format
                    await Task.Run(() => File.Copy(workingDocPath, outputPath, true));
                    return outputPath;
                    
                case ExportFormat.Html:
                case ExportFormat.EmailHtml:
                case ExportFormat.Pdf:
                    // Use the DocumentGenerationService for proper format conversion
                    try
                    {
                        var convertedPath = await _documentGenerationService.ConvertDocumentFormatAsync(workingDocPath, outputPath, exportFormat);
                        _logger.LogInformation("✅ Successfully converted embedded document to {ExportFormat}: {OutputPath}", exportFormat, convertedPath);
                        return convertedPath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Failed to convert embedded document to {ExportFormat}, falling back to Word format", exportFormat);
                        // Fallback to Word format if conversion fails
                        var fallbackPath = Path.ChangeExtension(outputPath, ".docx");
                        await Task.Run(() => File.Copy(workingDocPath, fallbackPath, true));
                        return fallbackPath;
                    }
                    
                default:
                    await Task.Run(() => File.Copy(workingDocPath, outputPath, true));
                    return outputPath;
            }
        }

        #region Helper Methods (from your original DocumentEmbeddingService)

        private OpenXmlElement? FindPlaceholderInWordDocument(WordprocessingDocument doc, string placeholder)
        {
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return null;

            // Look for placeholder in text content
            foreach (var paragraph in body.Descendants<Paragraph>())
            {
                var fullText = string.Join("", paragraph.Descendants<Text>().Select(t => t.Text));
                if (fullText.Contains(placeholder))
                {
                    return paragraph;
                }
            }

            // Look for placeholder in custom property fields
            foreach (var field in body.Descendants<SimpleField>())
            {
                var instruction = field.Instruction?.Value;
                if (!string.IsNullOrEmpty(instruction) && instruction.Contains(placeholder))
                {
                    return field.Parent;
                }
            }

            // Look for placeholder in complex fields (field codes)
            var fieldCodes = body.Descendants<FieldCode>();
            foreach (var fieldCode in fieldCodes)
            {
                if (fieldCode.Text != null && fieldCode.Text.Contains(placeholder))
                {
                    // Find the parent element that contains the entire field
                    var parent = fieldCode.Ancestors<Paragraph>().FirstOrDefault();
                    if (parent != null) return parent;
                }
            }

            return null;
        }

        private void ImportStyles(WordprocessingDocument mainDoc, WordprocessingDocument embedDoc)
        {
            try
            {
                if (embedDoc.MainDocumentPart?.StyleDefinitionsPart?.Styles == null) return;

                var mainStyles = mainDoc.MainDocumentPart?.StyleDefinitionsPart;
                if (mainStyles == null)
                {
                    mainStyles = mainDoc.MainDocumentPart?.AddNewPart<StyleDefinitionsPart>();
                    if (mainStyles != null)
                        mainStyles.Styles = new Styles();
                }

                if (mainStyles?.Styles == null) return;

                var embedStyles = embedDoc.MainDocumentPart.StyleDefinitionsPart.Styles;
                var existingStyleIds = mainStyles.Styles.Elements<Style>().Select(s => s.StyleId?.Value).ToHashSet();

                int importedCount = 0;
                foreach (var style in embedStyles.Elements<Style>())
                {
                    var styleId = style.StyleId?.Value;
                    if (!string.IsNullOrEmpty(styleId) && !existingStyleIds.Contains(styleId))
                    {
                        var clonedStyle = style.CloneNode(true) as Style;
                        if (clonedStyle != null)
                        {
                            mainStyles.Styles.AppendChild(clonedStyle);
                            existingStyleIds.Add(styleId);
                            importedCount++;
                        }
                    }
                }

                if (importedCount > 0)
                {
                    _logger.LogDebug("   ✓ Imported {StyleCount} styles from embed document", importedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not import styles: {Message}", ex.Message);
            }
        }

        private void ImportNumberingDefinitions(WordprocessingDocument mainDoc, WordprocessingDocument embedDoc)
        {
            try
            {
                if (embedDoc.MainDocumentPart?.NumberingDefinitionsPart?.Numbering == null) return;

                var mainNumbering = mainDoc.MainDocumentPart?.NumberingDefinitionsPart;
                if (mainNumbering == null)
                {
                    mainNumbering = mainDoc.MainDocumentPart?.AddNewPart<NumberingDefinitionsPart>();
                    if (mainNumbering != null)
                        mainNumbering.Numbering = new Numbering();
                }

                if (mainNumbering?.Numbering == null) return;

                var embedNumbering = embedDoc.MainDocumentPart.NumberingDefinitionsPart.Numbering;
                int importedCount = 0;
                
                // Import abstract numbering definitions
                foreach (var abstractNum in embedNumbering.Elements<AbstractNum>())
                {
                    var cloned = abstractNum.CloneNode(true) as AbstractNum;
                    if (cloned != null)
                    {
                        mainNumbering.Numbering.AppendChild(cloned);
                        importedCount++;
                    }
                }

                // Import numbering instances
                foreach (var numInstance in embedNumbering.Elements<NumberingInstance>())
                {
                    var cloned = numInstance.CloneNode(true) as NumberingInstance;
                    if (cloned != null)
                    {
                        mainNumbering.Numbering.AppendChild(cloned);
                        importedCount++;
                    }
                }

                if (importedCount > 0)
                {
                    _logger.LogDebug("   ✓ Imported {NumberingCount} numbering definitions from embed document", importedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not import numbering definitions: {Message}", ex.Message);
            }
        }

        private void ImportImages(WordprocessingDocument mainDoc, WordprocessingDocument embedDoc)
        {
            try
            {
                if (embedDoc.MainDocumentPart == null) return;

                var embedImageParts = embedDoc.MainDocumentPart.ImageParts.ToList();
                if (!embedImageParts.Any()) return;

                int importedCount = 0;
                foreach (var embedImagePart in embedImageParts)
                {
                    // Create new image part in main document
                    var newImagePart = mainDoc.MainDocumentPart?.AddImagePart(embedImagePart.ContentType);
                    if (newImagePart != null)
                    {
                        // Copy image data
                        using (var stream = embedImagePart.GetStream())
                        {
                            stream.CopyTo(newImagePart.GetStream(FileMode.Create));
                        }
                        importedCount++;
                    }
                }

                if (importedCount > 0)
                {
                    _logger.LogDebug("   ✓ Imported {ImageCount} images from embed document", importedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not import images: {Message}", ex.Message);
            }
        }

        private void UpdateCustomPropertiesInDocument(OpenXmlPackage package, Dictionary<string, string> propertyValues)
        {
            try
            {
                // Cast to WordprocessingDocument to access CustomFilePropertiesPart
                if (package is not WordprocessingDocument wordDoc) return;
                
                var customPropsPart = wordDoc.CustomFilePropertiesPart;
                if (customPropsPart?.Properties == null) return;

                foreach (var kvp in propertyValues)
                {
                    var existingProp = customPropsPart.Properties.Elements<DocumentFormat.OpenXml.CustomProperties.CustomDocumentProperty>()
                        .FirstOrDefault(p => p.Name?.Value == kvp.Key);

                    if (existingProp != null)
                    {
                        // Update existing property
                        if (existingProp.VTLPWSTR != null)
                            existingProp.VTLPWSTR.Text = kvp.Value;
                        else if (existingProp.VTFileTime != null)
                            existingProp.VTFileTime.Text = kvp.Value;
                        else if (existingProp.VTBool != null)
                            existingProp.VTBool.Text = kvp.Value;
                        else if (existingProp.VTInt32 != null)
                            existingProp.VTInt32.Text = kvp.Value;
                    }
                }

                customPropsPart.Properties.Save();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Warning updating custom properties: {Message}", ex.Message);
            }
        }

        private void RefreshWordDocPropertyFields(WordprocessingDocument doc, Dictionary<string, string> propertyValues)
        {
            try
            {
                var body = doc.MainDocumentPart?.Document?.Body;
                if (body == null) return;

                // Update simple fields (DOCPROPERTY fields)
                foreach (var field in body.Descendants<SimpleField>())
                {
                    var instruction = field.Instruction?.Value;
                    if (string.IsNullOrEmpty(instruction)) continue;

                    var match = Regex.Match(instruction, @"DOCPROPERTY\s+""?([^""}\s]+)""?", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var propertyName = match.Groups[1].Value;
                        if (propertyValues.TryGetValue(propertyName, out var newValue))
                        {
                            var textElement = field.Descendants<Text>().FirstOrDefault();
                            if (textElement != null)
                            {
                                textElement.Text = newValue;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Warning refreshing document fields: {Message}", ex.Message);
            }
        }

        private async Task CleanupTempFileAsync(string filePath, string description)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        _logger.LogDebug("🧹 Cleaned up {Description}: {FileName}", description, Path.GetFileName(filePath));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Could not clean up {Description} {FileName}: {Message}", description, Path.GetFileName(filePath), ex.Message);
                }
            });
        }

        #endregion

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

            return await File.ReadAllBytesAsync(document.FilePath);
        }

        private async Task<bool> CleanupSingleDocumentAsync(Guid generationId)
        {
            try
            {
                if (_generatedDocuments.TryGetValue(generationId, out var document))
                {
                    if (File.Exists(document.FilePath))
                    {
                        await Task.Run(() => File.Delete(document.FilePath));
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
    }
}

