using Microsoft.AspNetCore.Mvc;
using CMS.WebApi.Models;
using CMS.WebApi.Services;

namespace CMS.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new document in the CMS
        /// </summary>
        /// <param name="request">Document registration request with file content</param>
        /// <returns>Document ID and confirmation message</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterDocumentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RegisterDocument([FromForm] RegisterDocumentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _documentService.RegisterDocumentAsync(request);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid document registration request: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while registering document");
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Retrieve document metadata by ID
        /// </summary>
        /// <param name="id">Document ID</param>
        /// <returns>Document metadata with download URL</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(RetrieveDocumentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RetrieveDocument(Guid id)
        {
            try
            {
                var response = await _documentService.RetrieveDocumentAsync(id);
                
                if (response == null)
                {
                    return NotFound(new { error = "Document not found" });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving document {DocumentId}", id);
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Download document content by ID
        /// </summary>
        /// <param name="id">Document ID</param>
        /// <returns>Document file content</returns>
        [HttpGet("{id:guid}/download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DownloadDocument(Guid id)
        {
            try
            {
                var filePath = await _documentService.GetDocumentFilePathAsync(id);
                var fileName = Path.GetFileName(filePath);
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                
                // Determine content type based on file extension
                var contentType = GetContentType(filePath);
                
                return File(fileBytes, contentType, fileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { error = "Document or file not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while downloading document {DocumentId}", id);
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Get all documents in the CMS
        /// </summary>
        /// <returns>List of all documents with metadata and download URLs</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<RetrieveDocumentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllDocuments()
        {
            try
            {
                var documents = await _documentService.GetAllDocumentsAsync();
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all documents");
                return StatusCode(500, new { error = "Internal server error occurred" });
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
                ".zip" => "application/zip",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }
    }
}
