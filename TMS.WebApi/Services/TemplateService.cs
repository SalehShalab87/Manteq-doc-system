using CMS.WebApi.Models;
using CMS.WebApi.Services;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.CustomProperties;
using TMS.WebApi.Models;
using System.Text.Json;
using Microsoft.Extensions.Options;
using CmsTemplate = CMS.WebApi.Models.Template;
using CmsDocument = CMS.WebApi.Models.Document;

namespace TMS.WebApi.Services
{
    public interface ITemplateService
    {
        Task<RegisterTemplateResponse> RegisterTemplateAsync(RegisterTemplateRequest request);
        Task<RetrieveTemplateResponse?> GetTemplateAsync(Guid templateId);
        Task<List<RetrieveTemplateResponse>> GetAllTemplatesAsync();
        Task<TemplatePropertiesResponse?> GetTemplatePropertiesAsync(Guid templateId);
        Task<bool> DeleteTemplateAsync(Guid templateId);
        Task<List<string>> ExtractPlaceholdersFromFileAsync(IFormFile file);
        Task<Dictionary<string, string>> GetCustomPropertiesWithValuesAsync(string filePath);
        Task<Dictionary<string, string>> ExtractPlaceholdersFromStreamAsync(Stream stream, string extension);
    }

    public class TemplateService : ITemplateService
    {
        private readonly IDocumentService _cmsDocumentService;
        private readonly ICmsTemplateService _cmsTemplateService;
        private readonly ILogger<TemplateService> _logger;
        private readonly TmsSettings _tmsSettings;
        private readonly string _tempUploadPath;

        public TemplateService(
            IDocumentService cmsDocumentService,
            ICmsTemplateService cmsTemplateService,
            ILogger<TemplateService> logger,
            IOptions<TmsSettings> tmsSettings)
        {
            _cmsDocumentService = cmsDocumentService;
            _cmsTemplateService = cmsTemplateService;
            _logger = logger;
            
            // Create temp upload directory
            _tmsSettings = tmsSettings.Value;
            
            // Use shared temp upload path from configuration, fallback to current directory
            var tempPath = _tmsSettings.TempUploadPath ?? 
                          Path.Combine(Directory.GetCurrentDirectory(), "TempUploads");
            _tempUploadPath = tempPath;
            
            if (!Directory.Exists(_tempUploadPath))
            {
                Directory.CreateDirectory(_tempUploadPath);
            }
        }

        public async Task<RegisterTemplateResponse> RegisterTemplateAsync(RegisterTemplateRequest request)
        {
            try
            {
                _logger.LogInformation("Registering new template: {TemplateName}", request.Name);

                // Validate file type
                var fileExtension = Path.GetExtension(request.TemplateFile.FileName).ToLowerInvariant();
                if (!IsValidTemplateFile(fileExtension))
                {
                    throw new ArgumentException($"Unsupported template file type: {fileExtension}. Supported types: .docx, .xlsx, .pptx");
                }

                // Save uploaded file temporarily to extract placeholders
                string tempFilePath = await SaveTemporaryFileAsync(request.TemplateFile);
                
                try
                {
                    // Extract placeholders using OpenXML
                    var placeholders = await ExtractPlaceholdersFromPathAsync(tempFilePath);
                    _logger.LogInformation("Extracted {PlaceholderCount} placeholders from template", placeholders.Count);

                    // Register the template file in CMS Document system
                    var documentRequest = new RegisterDocumentRequest
                    {
                        Name = request.TemplateFile.FileName,
                        Type = GetDocumentType(fileExtension),
                        Content = request.TemplateFile
                    };

                    var documentResponse = await _cmsDocumentService.RegisterDocumentAsync(documentRequest);
                    _logger.LogInformation("Template file registered in CMS with ID: {DocumentId}", documentResponse.Id);

                    // Create Template record in CMS
                    var template = new CmsTemplate
                    {
                        Name = request.Name,
                        Description = request.Description,
                        Category = request.Category,
                        CmsDocumentId = documentResponse.Id,
                        Placeholders = placeholders,
                        CreatedBy = request.CreatedBy,
                        UpdatedBy = request.CreatedBy,
                        IsActive = true
                    };

                    var savedTemplate = await _cmsTemplateService.CreateTemplateAsync(template);
                    
                    _logger.LogInformation("Template registered successfully with ID: {TemplateId}", savedTemplate.Id);

                    return new RegisterTemplateResponse
                    {
                        TemplateId = savedTemplate.Id,
                        Message = $"Template '{request.Name}' registered successfully",
                        ExtractedPlaceholders = placeholders
                    };
                }
                finally
                {
                    // Clean up temporary file
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                        _logger.LogDebug("Cleaned up temporary file: {TempFilePath}", tempFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering template: {TemplateName}", request.Name);
                throw;
            }
        }

        public async Task<RetrieveTemplateResponse?> GetTemplateAsync(Guid templateId)
        {
            try
            {
                var template = await _cmsTemplateService.GetTemplateByIdAsync(templateId);
                if (template == null)
                {
                    return null;
                }

                // Get the associated document download URL
                var documentResponse = await _cmsDocumentService.RetrieveDocumentAsync(template.CmsDocumentId);
                
                return new RetrieveTemplateResponse
                {
                    Id = template.Id,
                    Name = template.Name,
                    Description = template.Description == null ? "" : template.Description,
                    Category = template.Category,
                    CmsDocumentId = template.CmsDocumentId,
                    Placeholders = template.Placeholders,
                    CreatedAt = template.CreatedAt,
                    UpdatedAt = template.UpdatedAt,
                    IsActive = template.IsActive,
                    CreatedBy = template.CreatedBy,
                    UpdatedBy = template.UpdatedBy == null ? "" : template.UpdatedBy,
                    TemplateDownloadUrl = documentResponse?.DownloadUrl ?? ""
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving template: {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<List<RetrieveTemplateResponse>> GetAllTemplatesAsync()
        {
            try
            {
                var templates = await _cmsTemplateService.GetAllTemplatesAsync();
                var responses = new List<RetrieveTemplateResponse>();

                foreach (var template in templates)
                {
                    var documentResponse = await _cmsDocumentService.RetrieveDocumentAsync(template.CmsDocumentId);
                    
                    responses.Add(new RetrieveTemplateResponse
                    {
                        Id = template.Id,
                        Name = template.Name,
                        Description = template.Description == null ? "" : template.Description,
                        Category = template.Category,
                        CmsDocumentId = template.CmsDocumentId,
                        Placeholders = template.Placeholders,
                        CreatedAt = template.CreatedAt,
                        UpdatedAt = template.UpdatedAt,
                        IsActive = template.IsActive,
                        CreatedBy = template.CreatedBy,
                        UpdatedBy = template.UpdatedBy == null ? "" : template.UpdatedBy,
                        TemplateDownloadUrl = documentResponse?.DownloadUrl ?? ""
                    });
                }

                return responses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all templates");
                throw;
            }
        }

        public async Task<TemplatePropertiesResponse?> GetTemplatePropertiesAsync(Guid templateId)
        {
            try
            {
                var template = await _cmsTemplateService.GetTemplateByIdAsync(templateId);
                if (template == null)
                {
                    return null;
                }

                // Get the template file to extract current property values
                var documentFilePath = await _cmsDocumentService.GetDocumentFilePathAsync(template.CmsDocumentId);
                var currentValues = await GetCustomPropertiesWithValuesAsync(documentFilePath);

                var properties = template.Placeholders.Select(placeholder => new TemplateProperty
                {
                    Name = placeholder,
                    Type = "Text", // Default to text, can be enhanced later
                    IsRequired = true, // Default to required, can be enhanced later
                    Description = $"Property for {placeholder}",
                    CurrentValue = currentValues.GetValueOrDefault(placeholder, "")
                }).ToList();

                return new TemplatePropertiesResponse
                {
                    TemplateId = template.Id,
                    TemplateName = template.Name,
                    Properties = properties
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving template properties: {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<bool> DeleteTemplateAsync(Guid templateId)
        {
            try
            {
                var template = await _cmsTemplateService.GetTemplateByIdAsync(templateId);
                if (template == null)
                {
                    return false;
                }

                // Delete the template from CMS (this might cascade to the document)
                var result = await _cmsTemplateService.DeleteTemplateAsync(templateId);
                
                _logger.LogInformation("Template deleted: {TemplateId}, Success: {Success}", templateId, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template: {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<List<string>> ExtractPlaceholdersFromFileAsync(IFormFile file)
        {
            string tempFilePath = await SaveTemporaryFileAsync(file);
            
            try
            {
                return await ExtractPlaceholdersFromPathAsync(tempFilePath);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        public async Task<Dictionary<string, string>> GetCustomPropertiesWithValuesAsync(string templatePath)
        {
            return await Task.Run(() => GetCustomPropertiesFromFile(templatePath));
        }

        public async Task<Dictionary<string, string>> ExtractPlaceholdersFromStreamAsync(Stream stream, string extension)
        {
            return await Task.Run(() =>
            {
                var properties = new Dictionary<string, string>();
                
                try
                {
                    switch (extension.ToLowerInvariant())
                    {
                        case ".docx":
                            using (var doc = WordprocessingDocument.Open(stream, false))
                            {
                                properties = GetCustomPropertiesFromOpenXml(doc);
                            }
                            break;

                        case ".xlsx":
                            using (var doc = SpreadsheetDocument.Open(stream, false))
                            {
                                properties = GetCustomPropertiesFromOpenXml(doc);
                            }
                            break;

                        case ".pptx":
                            using (var doc = PresentationDocument.Open(stream, false))
                            {
                                properties = GetCustomPropertiesFromOpenXml(doc);
                            }
                            break;

                        default:
                            throw new ArgumentException($"Unsupported file extension: {extension}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting placeholders from stream with extension {Extension}", extension);
                    throw;
                }

                return properties;
            });
        }

        private async Task<string> SaveTemporaryFileAsync(IFormFile file)
        {
            var fileName = $"temp_{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(_tempUploadPath, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return filePath;
        }

        private async Task<List<string>> ExtractPlaceholdersFromPathAsync(string filePath)
        {
            return await Task.Run(() => ExtractPlaceholdersFromFile(filePath));
        }

        private List<string> ExtractPlaceholdersFromFile(string filePath)
        {
            var placeholders = new HashSet<string>();
            var documentType = GetTmsDocumentTypeFromPath(filePath);

            try
            {
                switch (documentType)
                {
                    case TMS.WebApi.Models.DocumentType.Word:
                        using (var doc = WordprocessingDocument.Open(filePath, false))
                        {
                            var customProps = ExtractCustomPropertiesFromOpenXml(doc);
                            foreach (var prop in customProps)
                            {
                                placeholders.Add(prop);
                            }
                        }
                        break;

                    case TMS.WebApi.Models.DocumentType.Excel:
                        using (var doc = SpreadsheetDocument.Open(filePath, false))
                        {
                            var customProps = ExtractCustomPropertiesFromOpenXml(doc);
                            foreach (var prop in customProps)
                            {
                                placeholders.Add(prop);
                            }
                        }
                        break;

                    case TMS.WebApi.Models.DocumentType.PowerPoint:
                        using (var doc = PresentationDocument.Open(filePath, false))
                        {
                            var customProps = ExtractCustomPropertiesFromOpenXml(doc);
                            foreach (var prop in customProps)
                            {
                                placeholders.Add(prop);
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not extract placeholders from {FilePath}: {Message}", filePath, ex.Message);
            }

            return placeholders.ToList();
        }

        private Dictionary<string, string> GetCustomPropertiesFromFile(string filePath)
        {
            var properties = new Dictionary<string, string>();
            var documentType = GetTmsDocumentTypeFromPath(filePath);

            try
            {
                switch (documentType)
                {
                    case TMS.WebApi.Models.DocumentType.Word:
                        using (var doc = WordprocessingDocument.Open(filePath, false))
                        {
                            properties = GetCustomPropertiesFromOpenXml(doc);
                        }
                        break;

                    case TMS.WebApi.Models.DocumentType.Excel:
                        using (var doc = SpreadsheetDocument.Open(filePath, false))
                        {
                            properties = GetCustomPropertiesFromOpenXml(doc);
                        }
                        break;

                    case TMS.WebApi.Models.DocumentType.PowerPoint:
                        using (var doc = PresentationDocument.Open(filePath, false))
                        {
                            properties = GetCustomPropertiesFromOpenXml(doc);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not read custom properties from {FilePath}: {Message}", filePath, ex.Message);
            }

            return properties;
        }

        private List<string> ExtractCustomPropertiesFromOpenXml(OpenXmlPackage document)
        {
            var properties = new List<string>();

            try
            {
                CustomFilePropertiesPart? customPropertiesPart = null;
                
                if (document is WordprocessingDocument wordDoc)
                {
                    customPropertiesPart = wordDoc.CustomFilePropertiesPart;
                }
                else if (document is SpreadsheetDocument excelDoc)
                {
                    customPropertiesPart = excelDoc.CustomFilePropertiesPart;
                }
                else if (document is PresentationDocument pptDoc)
                {
                    customPropertiesPart = pptDoc.CustomFilePropertiesPart;
                }

                if (customPropertiesPart?.Properties != null)
                {
                    foreach (var prop in customPropertiesPart.Properties.Elements<CustomDocumentProperty>())
                    {
                        if (prop.Name?.Value != null)
                        {
                            properties.Add(prop.Name.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not read custom properties: {Message}", ex.Message);
            }

            return properties;
        }

        private Dictionary<string, string> GetCustomPropertiesFromOpenXml(OpenXmlPackage document)
        {
            var properties = new Dictionary<string, string>();

            try
            {
                CustomFilePropertiesPart? customPropertiesPart = null;
                
                if (document is WordprocessingDocument wordDoc)
                {
                    customPropertiesPart = wordDoc.CustomFilePropertiesPart;
                }
                else if (document is SpreadsheetDocument excelDoc)
                {
                    customPropertiesPart = excelDoc.CustomFilePropertiesPart;
                }
                else if (document is PresentationDocument pptDoc)
                {
                    customPropertiesPart = pptDoc.CustomFilePropertiesPart;
                }

                if (customPropertiesPart?.Properties != null)
                {
                    foreach (var prop in customPropertiesPart.Properties.Elements<CustomDocumentProperty>())
                    {
                        if (prop.Name?.Value != null)
                        {
                            string name = prop.Name.Value;
                            string value = GetPropertyValue(prop);
                            properties[name] = value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not read custom properties with values: {Message}", ex.Message);
            }

            return properties;
        }

        private string GetPropertyValue(CustomDocumentProperty prop)
        {
            try
            {
                if (prop.VTLPWSTR != null) return prop.VTLPWSTR.Text ?? "";
                if (prop.VTFileTime != null) return prop.VTFileTime.Text ?? "";
                if (prop.VTBool != null) return prop.VTBool.Text ?? "";
                if (prop.VTInt32 != null) return prop.VTInt32.Text ?? "";

                var firstChild = prop.FirstChild;
                if (firstChild != null)
                {
                    return firstChild.InnerText ?? "";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not read property value: {Message}", ex.Message);
            }
            
            return "";
        }

        private static bool IsValidTemplateFile(string fileExtension)
        {
            return fileExtension switch
            {
                ".docx" or ".xlsx" or ".pptx" => true,
                _ => false
            };
        }

        private static string GetDocumentType(string fileExtension)
        {
            return fileExtension switch
            {
                ".docx" => "Word Template",
                ".xlsx" => "Excel Template", 
                ".pptx" => "PowerPoint Template",
                _ => "Office Template"
            };
        }

        private static TMS.WebApi.Models.DocumentType GetTmsDocumentTypeFromPath(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".docx" => TMS.WebApi.Models.DocumentType.Word,
                ".xlsx" => TMS.WebApi.Models.DocumentType.Excel,
                ".pptx" => TMS.WebApi.Models.DocumentType.PowerPoint,
                _ => TMS.WebApi.Models.DocumentType.Word
            };
        }
    }
}
