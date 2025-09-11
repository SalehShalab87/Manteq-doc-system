using Microsoft.AspNetCore.Mvc;
using TMS.WebApi.Models;
using TMS.WebApi.Services;

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
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(
        ITemplateService templateService,
        IDocumentGenerationService documentGenerationService,
        IDocumentEmbeddingService documentEmbeddingService,
        ILogger<TemplatesController> logger)
    {
        _templateService = templateService;
        _documentGenerationService = documentGenerationService;
        _documentEmbeddingService = documentEmbeddingService;
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
                    
                    _logger.LogInformation("✅ Document downloaded successfully: {GenerationId}", generationId);
                    return File(fileBytes, contentType, fileName);
                }
                catch (FileNotFoundException)
                {
                    // Fall back to document embedding service
                    var fileBytes = await _documentEmbeddingService.DownloadGeneratedDocumentAsync(generationId);
                    var fileName = $"GeneratedDocumentWithEmbeddings_{generationId:N}.docx"; // Default filename
                    
                    _logger.LogInformation("✅ Document with embeddings downloaded successfully: {GenerationId}", generationId);
                    return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
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
                        var fileBytes = await _documentGenerationService.DownloadGeneratedDocumentAsync(result.GenerationId);
                        var filePath = await _documentGenerationService.GetGeneratedDocumentPathAsync(result.GenerationId);
                        var fileName = Path.GetFileName(filePath);
                        var contentType = GetContentType(filePath);
                        
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
    }
}
