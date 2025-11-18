using Microsoft.AspNetCore.Mvc;
using CMS.WebApi.Services;
using CMS.WebApi.Models;

namespace CMS.WebApi.Controllers
{
    /// <summary>
    /// Email Templates API - Manage email templates with various body types and attachments
    /// </summary>
    [ApiController]
    [Route("api/email-templates")]
    [Produces("application/json")]
    public class EmailTemplatesController : ControllerBase
    {
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IEmailTemplateFileService _fileService;
        private readonly ILogger<EmailTemplatesController> _logger;

        public EmailTemplatesController(
            IEmailTemplateService emailTemplateService,
            IEmailTemplateFileService fileService,
            ILogger<EmailTemplatesController> logger)
        {
            _emailTemplateService = emailTemplateService;
            _fileService = fileService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new email template
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(EmailTemplateResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateEmailTemplate([FromBody] CreateEmailTemplateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var createdBy = Request.Headers["X-User-Id"].FirstOrDefault() ?? "SYSTEM";
                var response = await _emailTemplateService.CreateEmailTemplateAsync(request, createdBy);

                _logger.LogInformation("📧 Email template created: {TemplateId} by {CreatedBy}", response.Id, createdBy);

                return CreatedAtAction(nameof(GetEmailTemplateById), new { id = response.Id }, response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("⚠️ Invalid email template request: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating email template");
                return StatusCode(500, new { error = "Internal server error occurred while creating email template" });
            }
        }

        /// <summary>
        /// Get all unique email template categories
        /// </summary>
        [HttpGet("categories")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var templates = await _emailTemplateService.GetAllEmailTemplatesAsync();
                var categories = templates
                    .Where(t => !string.IsNullOrEmpty(t.Category))
                    .Select(t => t.Category!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                _logger.LogDebug("✅ Retrieved {Count} unique categories", categories.Count);
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving email template categories");
                return StatusCode(500, new { error = "Internal server error occurred while retrieving categories" });
            }
        }

        /// <summary>
        /// Get email template by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(EmailTemplateResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmailTemplateById(Guid id)
        {
            try
            {
                var template = await _emailTemplateService.GetEmailTemplateByIdAsync(id);
                
                if (template == null)
                {
                    _logger.LogWarning("⚠️ Email template not found: {TemplateId}", id);
                    return NotFound(new { error = "Email template not found" });
                }

                _logger.LogDebug("✅ Email template retrieved: {TemplateId}", id);
                return Ok(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving email template: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred while retrieving email template" });
            }
        }

        /// <summary>
        /// Get all email templates with optional filtering
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<EmailTemplateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllEmailTemplates([FromQuery] bool? isActive = null, [FromQuery] string? category = null)
        {
            try
            {
                var templates = await _emailTemplateService.GetAllEmailTemplatesAsync(isActive, category);
                _logger.LogDebug("✅ Retrieved {Count} email templates", templates.Count);
                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving email templates");
                return StatusCode(500, new { error = "Internal server error occurred while retrieving email templates" });
            }
        }

        /// <summary>
        /// Update email template
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(EmailTemplateResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateEmailTemplate(Guid id, [FromBody] UpdateEmailTemplateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var template = await _emailTemplateService.UpdateEmailTemplateAsync(id, request);
                
                if (template == null)
                {
                    _logger.LogWarning("⚠️ Email template not found for update: {TemplateId}", id);
                    return NotFound(new { error = "Email template not found" });
                }

                _logger.LogInformation("📝 Email template updated: {TemplateId}", id);
                return Ok(template);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("⚠️ Invalid update request: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating email template: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred while updating email template" });
            }
        }

        /// <summary>
        /// Delete email template (soft delete)
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteEmailTemplate(Guid id)
        {
            try
            {
                var deleted = await _emailTemplateService.DeleteEmailTemplateAsync(id);
                
                if (!deleted)
                {
                    _logger.LogWarning("⚠️ Email template not found for deletion: {TemplateId}", id);
                    return NotFound(new { error = "Email template not found" });
                }

                _logger.LogInformation("🗑️ Email template deleted: {TemplateId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting email template: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred while deleting email template" });
            }
        }

        /// <summary>
        /// Activate email template
        /// </summary>
        [HttpPost("{id:guid}/activate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ActivateEmailTemplate(Guid id)
        {
            try
            {
                var activated = await _emailTemplateService.ActivateEmailTemplateAsync(id);
                
                if (!activated)
                {
                    _logger.LogWarning("⚠️ Email template not found for activation: {TemplateId}", id);
                    return NotFound(new { error = "Email template not found" });
                }

                _logger.LogInformation("🟢 Email template activated: {TemplateId}", id);
                return Ok(new { message = "Email template activated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error activating email template: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred while activating email template" });
            }
        }

        /// <summary>
        /// Deactivate email template
        /// </summary>
        [HttpPost("{id:guid}/deactivate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeactivateEmailTemplate(Guid id)
        {
            try
            {
                var deactivated = await _emailTemplateService.DeactivateEmailTemplateAsync(id);
                
                if (!deactivated)
                {
                    _logger.LogWarning("⚠️ Email template not found for deactivation: {TemplateId}", id);
                    return NotFound(new { error = "Email template not found" });
                }

                _logger.LogInformation("🔴 Email template deactivated: {TemplateId}", id);
                return Ok(new { message = "Email template deactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deactivating email template: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred while deactivating email template" });
            }
        }

        /// <summary>
        /// Get email template analytics
        /// </summary>
        [HttpGet("{id:guid}/analytics")]
        [ProducesResponseType(typeof(EmailTemplateAnalyticsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmailTemplateAnalytics(Guid id)
        {
            try
            {
                var analytics = await _emailTemplateService.GetEmailTemplateAnalyticsAsync(id);
                
                if (analytics == null)
                {
                    _logger.LogWarning("⚠️ Email template not found for analytics: {TemplateId}", id);
                    return NotFound(new { error = "Email template not found" });
                }

                _logger.LogDebug("📊 Email template analytics retrieved: {TemplateId}", id);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving email template analytics: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred while retrieving analytics" });
            }
        }

        /// <summary>
        /// Get custom template content (HTML file)
        /// </summary>
        [HttpGet("{id:guid}/custom-template")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCustomTemplateContent(Guid id)
        {
            try
            {
                var htmlBytes = await _fileService.GetCustomTemplateAsync(id);
                _logger.LogDebug("✅ Custom template content retrieved: {TemplateId} ({Size} bytes)", id, htmlBytes.Length);
                return File(htmlBytes, "text/html");
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning("⚠️ Custom template not found: {TemplateId}", id);
                return NotFound(new { error = "Custom template not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving custom template: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred while retrieving custom template" });
            }
        }

        /// <summary>
        /// Upload custom template file (mHTML format)
        /// </summary>
        [HttpPost("{id:guid}/upload-custom")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadCustomTemplate(Guid id, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file provided" });
                }

                // Validate file type
                var allowedExtensions = new[] { ".html", ".xhtml", ".mhtml", ".mht" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { error = "Only .html, .xhtml, .mhtml and .mht files are allowed" });
                }

                // Upload file
                await _fileService.SaveCustomTemplateAsync(file, id);

                _logger.LogInformation("📤 Custom template file uploaded: {TemplateId}, File={FileName} ({Size} bytes)", 
                    id, file.FileName, file.Length);

                return Ok(new { message = "Custom template file uploaded successfully" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("⚠️ Invalid file: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error uploading custom template: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred while uploading custom template" });
            }
        }

        /// <summary>
        /// Get template attachments metadata
        /// </summary>
        [HttpGet("{id:guid}/attachments")]
        [ProducesResponseType(typeof(List<EmailTemplateAttachment>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTemplateAttachments(Guid id)
        {
            try
            {
                var attachments = await _emailTemplateService.GetTemplateAttachmentsAsync(id);
                _logger.LogDebug("✅ Retrieved {Count} attachments for template: {TemplateId}", attachments.Count, id);
                return Ok(attachments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving template attachments: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred while retrieving attachments" });
            }
        }

        /// <summary>
        /// Download custom attachment file
        /// </summary>
        [HttpGet("{id:guid}/attachments/{attachmentIndex:int}/download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DownloadCustomAttachment(Guid id, int attachmentIndex)
        {
            try
            {
                var (fileBytes, fileName, contentType) = await _fileService.GetCustomAttachmentAsync(id, attachmentIndex);
                
                _logger.LogDebug("📥 Custom attachment downloaded: {TemplateId}, Index={Index}, File={FileName} ({Size} bytes)", 
                    id, attachmentIndex, fileName, fileBytes.Length);

                return File(fileBytes, contentType, fileName);
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning("⚠️ Custom attachment not found: {TemplateId}, Index={Index}", id, attachmentIndex);
                return NotFound(new { error = "Custom attachment not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error downloading custom attachment: {TemplateId}, Index={Index}", id, attachmentIndex);
                return StatusCode(500, new { error = "Internal server error occurred while downloading attachment" });
            }
        }

        /// <summary>
        /// Increment sent count (for analytics)
        /// </summary>
        [HttpPost("{id:guid}/increment-sent")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> IncrementSentCount(Guid id)
        {
            try
            {
                var incremented = await _emailTemplateService.IncrementSentCountAsync(id);
                
                if (!incremented)
                {
                    _logger.LogWarning("⚠️ Email template not found for increment: {TemplateId}", id);
                    return NotFound(new { error = "Email template not found" });
                }

                _logger.LogDebug("📈 Sent count incremented for template: {TemplateId}", id);
                return Ok(new { message = "Sent count incremented successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error incrementing sent count: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Increment failure count (for analytics)
        /// </summary>
        [HttpPost("{id:guid}/increment-failure")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> IncrementFailureCount(Guid id)
        {
            try
            {
                var incremented = await _emailTemplateService.IncrementFailureCountAsync(id);
                
                if (!incremented)
                {
                    _logger.LogWarning("⚠️ Email template not found for increment: {TemplateId}", id);
                    return NotFound(new { error = "Email template not found" });
                }

                _logger.LogDebug("📈 Failure count incremented for template: {TemplateId}", id);
                return Ok(new { message = "Failure count incremented successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error incrementing failure count: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }
    }
}
