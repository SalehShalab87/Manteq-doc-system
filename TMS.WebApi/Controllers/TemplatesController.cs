using Microsoft.AspNetCore.Mvc;
using TMS.WebApi.Models;
using TMS.WebApi.Services;
using TMS.WebApi.HttpClients;

namespace TMS.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class TemplatesController : ControllerBase
    {
    private readonly ITemplateService _templateService;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly IDocumentEmbeddingService _documentEmbeddingService;
    private readonly ICmsApiClient _cmsApiClient;
    private readonly IExcelService _excelService;
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(
        ITemplateService templateService,
        IDocumentGenerationService documentGenerationService,
        IDocumentEmbeddingService documentEmbeddingService,
        ICmsApiClient cmsApiClient,
        IExcelService excelService,
        ILogger<TemplatesController> logger)
    {
        _templateService = templateService;
        _documentGenerationService = documentGenerationService;
        _documentEmbeddingService = documentEmbeddingService;
        _cmsApiClient = cmsApiClient;
        _excelService = excelService;
        _logger = logger;
    }        /// <summary>
        /// API 1: Register Template
        /// Registers a new template (Word, Excel, or PowerPoint) in the system.
        /// Extracts custom document properties using OpenXML and stores template metadata in CMS.
        /// </summary>
        /// <param name="request">Template registration request with file upload</param>
        /// <returns>Template registration response with extracted placeholders</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterTemplateResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RegisterTemplate([FromForm] RegisterTemplateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Registering template: {TemplateName} by {CreatedBy}", request.Name, request.CreatedBy);

                var response = await _templateService.RegisterTemplateAsync(request);
                
                _logger.LogInformation("Template registered successfully: {TemplateId} with {PlaceholderCount} placeholders", 
                    response.TemplateId, response.ExtractedPlaceholders.Count);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid template registration request: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while registering template");
                return StatusCode(500, new { error = "Internal server error occurred while registering template" });
            }
        }

        /// <summary>
        /// API 2: Retrieve Template
        /// Retrieves template metadata including basic information and download URL.
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <returns>Template metadata with download URL</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(RetrieveTemplateResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RetrieveTemplate(Guid id)
        {
            try
            {
                var template = await _templateService.GetTemplateAsync(id);
                
                if (template == null)
                {
                    return NotFound(new { error = "Template not found" });
                }

                _logger.LogDebug("Retrieved template: {TemplateId} - {TemplateName}", id, template.Name);

                return Ok(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving template {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred while retrieving template" });
            }
        }

        /// <summary>
        /// API 3: Retrieve Template Properties
        /// Retrieves all custom document properties (placeholders) available in the template.
        /// Shows property names, types, and current values from the template file.
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <returns>Template properties with current values</returns>
        [HttpGet("{id:guid}/properties")]
        [ProducesResponseType(typeof(TemplatePropertiesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RetrieveTemplateProperties(Guid id)
        {
            try
            {
                var properties = await _templateService.GetTemplatePropertiesAsync(id);
                
                if (properties == null)
                {
                    return NotFound(new { error = "Template not found" });
                }

                _logger.LogDebug("Retrieved {PropertyCount} properties for template: {TemplateId}", 
                    properties.Properties.Count, id);

                return Ok(properties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving template properties {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred while retrieving template properties" });
            }
        }

        /// <summary>
        /// API 4: Create Document From Template
        /// Generates a document from a template by:
        /// 1. Taking template ID and property values
        /// 2. Replacing custom document properties using OpenXML
        /// 3. Refreshing DOCPROPERTY fields in the document
        /// 4. Converting to requested export format (Original, Word, HTML, Email HTML, PDF)
        /// 5. Returning temporary download link that expires in 24 hours
        /// </summary>
        /// <param name="request">Document generation request with template ID, property values, and export format</param>
        /// <param name="autoDownload">Optional: if true, returns the file directly instead of metadata</param>
        /// <returns>Generated document information with download URL, or direct file download if autoDownload=true</returns>
        [HttpPost("generate")]
        [ProducesResponseType(typeof(DocumentGenerationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateDocumentFromTemplate(
            [FromBody] DocumentGenerationRequest request,
            [FromQuery] bool autoDownload = false)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Generating document from template: {TemplateId} with {PropertyCount} properties, Export format: {ExportFormat}, Auto-download: {AutoDownload}", 
                    request.TemplateId, request.PropertyValues.Count, request.ExportFormat, autoDownload);

                var response = await _documentGenerationService.GenerateDocumentAsync(request);
                
                _logger.LogInformation("Document generated successfully: {GenerationId} - {FileName} ({FileSize} bytes)", 
                    response.GenerationId, response.FileName, response.FileSizeBytes);

                // If autoDownload is requested, return the file directly
                if (autoDownload)
                {
                    try
                    {
                        var fileBytes = await _documentGenerationService.DownloadGeneratedDocumentAsync(response.GenerationId);
                        var filePath = await _documentGenerationService.GetGeneratedDocumentPathAsync(response.GenerationId);
                        var fileName = Path.GetFileName(filePath);
                        var contentType = GetContentType(filePath);

                        // Expose generation id to caller in the response headers when streaming file
                        // so clients can correlate the download with the generated metadata.
                        try
                        {
                            Response.Headers["X-Generation-Id"] = response.GenerationId.ToString();
                        }
                        catch { /* ignore header write failures */ }

                        _logger.LogInformation("📥 Auto-downloading generated document: {FileName} ({FileSize} KB)", 
                            fileName, fileBytes.Length / 1024);

                        return File(fileBytes, contentType, fileName);
                    }
                    catch (Exception downloadEx)
                    {
                        _logger.LogError(downloadEx, "Error during auto-download for generation {GenerationId}", response.GenerationId);
                        // Fall back to returning metadata if auto-download fails
                        return Ok(response);
                    }
                }

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid document generation request: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning("Template not found: {Message}", ex.Message);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating document from template {TemplateId}", request.TemplateId);
                return StatusCode(500, new { error = "Internal server error occurred while generating document" });
            }
        }

        /// <summary>
        /// Download Generated Document
        /// Downloads a temporarily generated document using the generation ID.
        /// Files are automatically deleted after 24 hours or after download.
        /// </summary>
        /// <param name="generationId">Generation ID from document creation response</param>
        /// <returns>Generated document file</returns>
        [HttpGet("download/{generationId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DownloadGeneratedDocument(Guid generationId)
        {
            try
            {
                _logger.LogInformation("📥 Request to download generated document: {GenerationId}", generationId);

                // Try regular document generation service first
                try
                {
                    var fileBytes = await _documentGenerationService.DownloadGeneratedDocumentAsync(generationId);
                    var filePath = await _documentGenerationService.GetGeneratedDocumentPathAsync(generationId);
                    var fileName = Path.GetFileName(filePath);
                    var contentType = GetContentType(filePath);
                    
                    _logger.LogInformation("✅ Regular document downloaded successfully: {GenerationId} - {FileName}", generationId, fileName);
                    return File(fileBytes, contentType, fileName);
                }
                catch (FileNotFoundException)
                {
                    _logger.LogDebug("Regular document not found, trying embedded document service for: {GenerationId}", generationId);
                    
                    // Try document embedding service
                    try
                    {
                        var fileBytes = await _documentEmbeddingService.DownloadGeneratedDocumentAsync(generationId);
                        
                        // Try to get a better filename by checking if we have the document info
                        // For now, use a default pattern
                        var fileName = $"EmbeddedDocument_{generationId:N}[0..8].pdf";
                        var contentType = "application/pdf"; // Default to PDF since most embeddings are PDF
                        
                        _logger.LogInformation("✅ Embedded document downloaded successfully: {GenerationId} - {FileName}", generationId, fileName);
                        return File(fileBytes, contentType, fileName);
                    }
                    catch (FileNotFoundException)
                    {
                        _logger.LogWarning("Document not found in both services: {GenerationId}", generationId);
                        throw; // Re-throw the exception preserving stack trace
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning("Generated document not found: {GenerationId}, Error: {Message}", generationId, ex.Message);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading generated document: {GenerationId}", generationId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while downloading the document" });
            }
        }

        /// <summary>
        /// Get All Templates
        /// Retrieves all templates in the system for administrative purposes.
        /// </summary>
        /// <returns>List of all templates</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<RetrieveTemplateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllTemplates()
        {
            try
            {
                var templates = await _templateService.GetAllTemplatesAsync();
                
                _logger.LogDebug("Retrieved {TemplateCount} templates", templates.Count);

                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all templates");
                return StatusCode(500, new { error = "Internal server error occurred while retrieving templates" });
            }
        }

        /// <summary>
        /// Delete Template
        /// Deletes a template and its associated file from the system.
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <returns>Deletion confirmation</returns>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteTemplate(Guid id)
        {
            try
            {
                var result = await _templateService.DeleteTemplateAsync(id);
                
                if (!result)
                {
                    return NotFound(new { error = "Template not found" });
                }

                _logger.LogInformation("Template deleted: {TemplateId}", id);

                return Ok(new { message = "Template deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting template {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred while deleting template" });
            }
        }

        private static string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                ".html" => "text/html",
                ".xml" => "application/xml",
                ".json" => "application/json",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// Get template types enum values for Angular dropdowns
        /// </summary>
        [HttpGet("template-types")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetTemplateTypes()
        {
            var types = Enum.GetValues(typeof(Models.DocumentType))
                .Cast<Models.DocumentType>()
                .Select(t => new { Value = (int)t, Name = t.ToString() })
                .ToList();
            
            return Ok(types);
        }

        /// <summary>
        /// Get export formats enum values for Angular dropdowns
        /// </summary>
        [HttpGet("export-formats")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetExportFormats()
        {
            var formats = Enum.GetValues(typeof(TMS.WebApi.Models.ExportFormat))
                .Cast<TMS.WebApi.Models.ExportFormat>()
                .Select(f => new { Value = (int)f, Name = f.ToString() })
                .ToList();
            
            return Ok(formats);
        }

        /// <summary>
        /// Get template analytics including success/failure statistics
        /// </summary>
        [HttpGet("{id:guid}/analytics")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTemplateAnalytics(Guid id)
        {
            try
            {
                var template = await _cmsApiClient.GetTemplateByIdAsync(id);
                if (template == null)
                    return NotFound(new { error = "Template not found" });
                
                var totalGenerations = template.SuccessCount + template.FailureCount;
                var successRate = totalGenerations > 0 
                    ? (double)template.SuccessCount / totalGenerations * 100 
                    : 0;
                
                return Ok(new
                {
                    TemplateId = template.Id,
                    TemplateName = template.Name,
                    SuccessCount = template.SuccessCount,
                    FailureCount = template.FailureCount,
                    TotalGenerations = totalGenerations,
                    SuccessRate = Math.Round(successRate, 2)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving template analytics: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Increment success count for template (called internally after successful generation)
        /// </summary>
        [HttpPost("{id:guid}/increment-success")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> IncrementSuccessCount(Guid id)
        {
            try
            {
                var result = await _cmsApiClient.IncrementSuccessCountAsync(id);
                if (!result)
                    return NotFound(new { error = "Template not found" });
                
                return Ok(new { message = "Success count incremented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing success count: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Increment failure count for template (called internally after failed generation)
        /// </summary>
        [HttpPost("{id:guid}/increment-failure")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> IncrementFailureCount(Guid id)
        {
            try
            {
                var result = await _cmsApiClient.IncrementFailureCountAsync(id);
                if (!result)
                    return NotFound(new { error = "Template not found" });
                
                return Ok(new { message = "Failure count incremented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing failure count: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// 5. Generate Document With Embeddings - Creates a document from a main template with embedded sub-templates
        /// Powerful feature that allows compositing multiple templates into a single document
        /// </summary>
        /// <param name="request">Document embedding request with main template and sub-templates</param>
        /// <param name="autoDownload">Optional: if true, returns the file directly instead of metadata</param>
        /// <returns>Document embedding response with download information, or direct file download</returns>
        [HttpPost("generate-with-embeddings")]
        [ProducesResponseType(typeof(DocumentEmbeddingResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DocumentEmbeddingResponse>> GenerateDocumentWithEmbeddings(
            [FromBody] DocumentEmbeddingRequest request,
            [FromQuery] bool autoDownload = false)
        {
            try
            {
                _logger.LogInformation("🔄 Received request to generate document with embeddings for template: {MainTemplateId}, Auto-download: {AutoDownload}", 
                    request.MainTemplateId, autoDownload);

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _documentEmbeddingService.GenerateDocumentWithEmbeddingAsync(request);
                
                _logger.LogInformation("✅ Document with embeddings generated successfully: {GenerationId}", result.GenerationId);
                
                // If autoDownload is requested, return the file directly
                if (autoDownload && result.GenerationId != Guid.Empty)
                {
                    try
                    {
                        var fileBytes = await _documentEmbeddingService.DownloadGeneratedDocumentAsync(result.GenerationId);
                        var fileName = result.FileName ?? $"EmbeddedDocument_{result.GenerationId:N}.pdf";
                        var contentType = GetContentType(fileName);
                        
                        try
                        {
                            Response.Headers["X-Generation-Id"] = result.GenerationId.ToString();
                        }
                        catch { }

                        _logger.LogInformation("📥 Auto-downloading embedded document: {FileName} ({FileSize} KB)", 
                            fileName, fileBytes.Length / 1024);

                        return File(fileBytes, contentType, fileName);
                    }
                    catch (Exception downloadEx)
                    {
                        _logger.LogError(downloadEx, "Error during auto-download for embedded generation {GenerationId}", result.GenerationId);
                        // Fall back to returning metadata if auto-download fails
                    }
                }
                
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid argument for document embedding generation: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating document with embeddings for template: {MainTemplateId}", request.MainTemplateId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while generating the document with embeddings" });
            }
        }

        /// <summary>
        /// Download placeholders as Excel file for offline editing
        /// </summary>
        [HttpGet("{id:guid}/download-placeholders-excel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DownloadPlaceholdersExcel(Guid id)
        {
            try
            {
                var properties = await _templateService.GetTemplatePropertiesAsync(id);
                if (properties == null)
                    return NotFound(new { error = "Template not found" });

                var placeholders = properties.Properties
                    .ToDictionary(p => p.Name, p => p.CurrentValue ?? string.Empty);
                var excelBytes = await _excelService.GeneratePlaceholdersExcelWithValuesAsync(placeholders);

                var fileName = $"Placeholders_{properties.TemplateName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                _logger.LogInformation("Generated placeholders Excel for template: {TemplateId} with {Count} placeholders", 
                    id, placeholders.Count);

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating placeholders Excel: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Extract placeholders from a template file without saving it
        /// Returns Excel file with placeholder names and their default values
        /// </summary>
        [HttpPost("extract-placeholders")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExtractPlaceholdersFromFile([FromForm] TemplateFileUploadRequest request)
        {
            try
            {
                if (request.TemplateFile == null || request.TemplateFile.Length == 0)
                    return BadRequest(new { error = "Template file is required" });

                var extension = Path.GetExtension(request.TemplateFile.FileName).ToLowerInvariant();
                if (extension != ".docx" && extension != ".xlsx")
                    return BadRequest(new { error = "Only .docx and .xlsx files are supported" });

                // Extract placeholders using the template service
                Dictionary<string, string> placeholders;
                using (var stream = request.TemplateFile.OpenReadStream())
                {
                    placeholders = await _templateService.ExtractPlaceholdersFromStreamAsync(stream, extension);
                }

                if (placeholders.Count == 0)
                {
                    _logger.LogWarning("No placeholders found in uploaded template file");
                    return BadRequest(new { error = "No custom properties (placeholders) found in the template file" });
                }

                // Generate Excel with placeholders and their default values
                var excelBytes = await _excelService.GeneratePlaceholdersExcelWithValuesAsync(placeholders);
                var fileName = $"Placeholders_{Path.GetFileNameWithoutExtension(request.TemplateFile.FileName)}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                _logger.LogInformation("Extracted {Count} placeholders from template file: {FileName}", 
                    placeholders.Count, request.TemplateFile.FileName);

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting placeholders from template file");
                return StatusCode(500, new { error = "Internal server error occurred while extracting placeholders" });
            }
        }

        /// <summary>
        /// Test template without saving - Generate document from template file and filled Excel
        /// Uploads template file and Excel with values, generates document without storing template
        /// </summary>
        [HttpPost("test-template")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> TestTemplateWithoutSaving([FromForm] TestTemplateRequest request)
        {
            try
            {
                if (request.TemplateFile == null || request.TemplateFile.Length == 0)
                    return BadRequest(new { error = "Template file is required" });

                if (request.ExcelFile == null || request.ExcelFile.Length == 0)
                    return BadRequest(new { error = "Excel file with test data is required" });

                var templateExtension = Path.GetExtension(request.TemplateFile.FileName).ToLowerInvariant();
                if (templateExtension != ".docx" && templateExtension != ".xlsx")
                    return BadRequest(new { error = "Only .docx and .xlsx template files are supported" });

                if (!request.ExcelFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { error = "Only .xlsx files are supported for test data" });

                _logger.LogInformation("Testing template: {TemplateFileName} with Excel: {ExcelFileName}", 
                    request.TemplateFile.FileName, request.ExcelFile.FileName);

                // Read Excel to get property values
                Dictionary<string, string> propertyValues;
                using (var stream = request.ExcelFile.OpenReadStream())
                {
                    propertyValues = await _excelService.ReadExcelToJsonAsync(stream);
                }

                if (propertyValues.Count == 0)
                    return BadRequest(new { error = "No property values found in Excel file" });

                // Generate document directly from template file without saving
                var response = await _documentGenerationService.GenerateDocumentFromFileAsync(
                    request.TemplateFile, 
                    propertyValues, 
                    request.ExportFormat);

                _logger.LogInformation("Test document generated: {FileName} with {PropertyCount} properties", 
                    response.FileName, propertyValues.Count);

                // Auto-download the generated file
                var fileBytes = await _documentGenerationService.DownloadGeneratedDocumentAsync(response.GenerationId);
                var contentType = GetContentType(response.FileName);
                try
                {
                    Response.Headers["X-Generation-Id"] = response.GenerationId.ToString();
                }
                catch { }

                return File(fileBytes, contentType, response.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing template without saving");
                return StatusCode(500, new { error = "Internal server error occurred while testing template" });
            }
        }

        /// <summary>
        /// Test generate document by uploading filled Excel file
        /// Upload Excel with placeholder values, system generates document without storing Excel
        /// </summary>
        [HttpPost("{id:guid}/test-generate")]
        [ProducesResponseType(typeof(DocumentGenerationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> TestGenerateWithExcel(
            Guid id,
            [FromForm] TestGenerateRequest request,
            [FromQuery] bool autoDownload = false)
        {
            try
            {
                if (request.ExcelFile == null || request.ExcelFile.Length == 0)
                    return BadRequest(new { error = "Excel file is required" });

                if (!request.ExcelFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { error = "Only .xlsx files are supported" });

                // Read Excel and convert to property values dictionary
                Dictionary<string, string> propertyValues;
                using (var stream = request.ExcelFile.OpenReadStream())
                {
                    propertyValues = await _excelService.ReadExcelToJsonAsync(stream);
                }

                if (propertyValues.Count == 0)
                    return BadRequest(new { error = "No property values found in Excel file" });

                // Generate document using the property values
                var generateRequest = new DocumentGenerationRequest
                {
                    TemplateId = id,
                    PropertyValues = propertyValues,
                    ExportFormat = request.ExportFormat,
                    GeneratedBy = "ExcelTest"
                };

                var response = await _documentGenerationService.GenerateDocumentAsync(generateRequest);

                _logger.LogInformation("Test document generated from Excel: {TemplateId}, {PropertyCount} properties", 
                    id, propertyValues.Count);

                // If autoDownload is requested, return the file directly
                if (autoDownload)
                {
                    try
                    {
                        var fileBytes = await _documentGenerationService.DownloadGeneratedDocumentAsync(response.GenerationId);
                        var contentType = GetContentType(response.FileName);
                        
                        try
                        {
                            Response.Headers["X-Generation-Id"] = response.GenerationId.ToString();
                        }
                        catch { }

                        _logger.LogInformation("📥 Auto-downloading test document: {FileName}", response.FileName);

                        return File(fileBytes, contentType, response.FileName);
                    }
                    catch (Exception downloadEx)
                    {
                        _logger.LogError(downloadEx, "Error during auto-download for test generation {GenerationId}", response.GenerationId);
                        // Fall back to returning metadata if auto-download fails
                    }
                }

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid Excel file or template: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error test generating document with Excel: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Parse Excel file and return property values as JSON
        /// Useful for frontend to extract property values from uploaded Excel files
        /// </summary>
        [HttpPost("parse-excel")]
        [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ParseExcelToJson([FromForm] ParseExcelRequest request)
        {
            try
            {
                if (request?.ExcelFile == null || request.ExcelFile.Length == 0)
                    return BadRequest(new { error = "Excel file is required" });

                if (!request.ExcelFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { error = "Only .xlsx files are supported" });

                // Read Excel and convert to property values dictionary
                Dictionary<string, string> propertyValues;
                using (var stream = request.ExcelFile.OpenReadStream())
                {
                    propertyValues = await _excelService.ReadExcelToJsonAsync(stream);
                }

                if (propertyValues.Count == 0)
                    return BadRequest(new { error = "No property values found in Excel file" });

                _logger.LogInformation("📊 Parsed Excel file: {FileName}, {PropertyCount} properties", 
                    request.ExcelFile.FileName, propertyValues.Count);

                return Ok(propertyValues);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid Excel file: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Excel file: {FileName}", request?.ExcelFile?.FileName);
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }
    }
}
