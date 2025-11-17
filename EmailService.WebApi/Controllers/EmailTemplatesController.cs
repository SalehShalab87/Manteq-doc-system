using Microsoft.AspNetCore.Mvc;
using CMS.WebApi.Services;
using CMS.WebApi.Models;

namespace EmailService.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailTemplatesController : ControllerBase
    {
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<EmailTemplatesController> _logger;

        public EmailTemplatesController(
            IEmailTemplateService emailTemplateService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<EmailTemplatesController> logger)
        {
            _emailTemplateService = emailTemplateService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.Request.Headers["X-SME-UserId"].FirstOrDefault() ?? "SYSTEM";
        }

        /// <summary>
        /// Create a new email template
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(EmailTemplateResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateEmailTemplate([FromBody] CreateEmailTemplateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var createdBy = GetCurrentUserId();
                var response = await _emailTemplateService.CreateEmailTemplateAsync(request, createdBy);

                _logger.LogInformation("Email template created: {TemplateId} by {CreatedBy}", response.Id, createdBy);

                return CreatedAtAction(nameof(GetEmailTemplate), new { id = response.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating email template");
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Get email template by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(EmailTemplateResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEmailTemplate(Guid id)
        {
            try
            {
                var template = await _emailTemplateService.GetEmailTemplateByIdAsync(id);
                if (template == null)
                    return NotFound(new { error = "Email template not found" });

                return Ok(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email template: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Get all email templates with optional filtering
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<EmailTemplateResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllEmailTemplates(
            [FromQuery] bool? isActive = null,
            [FromQuery] string? category = null)
        {
            try
            {
                var templates = await _emailTemplateService.GetAllEmailTemplatesAsync(isActive, category);
                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email templates");
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Update an email template
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(EmailTemplateResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEmailTemplate(Guid id, [FromBody] UpdateEmailTemplateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var template = await _emailTemplateService.UpdateEmailTemplateAsync(id, request);
                if (template == null)
                    return NotFound(new { error = "Email template not found" });

                return Ok(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating email template: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Delete an email template
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteEmailTemplate(Guid id)
        {
            try
            {
                var result = await _emailTemplateService.DeleteEmailTemplateAsync(id);
                if (!result)
                    return NotFound(new { error = "Email template not found" });

                return Ok(new { message = "Email template deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting email template: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Activate an email template
        /// </summary>
        [HttpPost("{id:guid}/activate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActivateEmailTemplate(Guid id)
        {
            try
            {
                var result = await _emailTemplateService.ActivateEmailTemplateAsync(id);
                if (!result)
                    return NotFound(new { error = "Email template not found" });

                return Ok(new { message = "Email template activated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating email template: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Deactivate an email template
        /// </summary>
        [HttpPost("{id:guid}/deactivate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateEmailTemplate(Guid id)
        {
            try
            {
                var result = await _emailTemplateService.DeactivateEmailTemplateAsync(id);
                if (!result)
                    return NotFound(new { error = "Email template not found" });

                return Ok(new { message = "Email template deactivated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating email template: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Get email template analytics
        /// </summary>
        [HttpGet("{id:guid}/analytics")]
        [ProducesResponseType(typeof(EmailTemplateAnalyticsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEmailTemplateAnalytics(Guid id)
        {
            try
            {
                var analytics = await _emailTemplateService.GetEmailTemplateAnalyticsAsync(id);
                if (analytics == null)
                    return NotFound(new { error = "Email template not found" });

                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email template analytics: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Increment sent count (called internally after successful email send)
        /// </summary>
        [HttpPost("{id:guid}/increment-sent")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> IncrementSentCount(Guid id)
        {
            try
            {
                var result = await _emailTemplateService.IncrementSentCountAsync(id);
                if (!result)
                    return NotFound(new { error = "Email template not found" });

                return Ok(new { message = "Sent count incremented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing sent count: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Increment failure count (called internally after failed email send)
        /// </summary>
        [HttpPost("{id:guid}/increment-failure")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> IncrementFailureCount(Guid id)
        {
            try
            {
                var result = await _emailTemplateService.IncrementFailureCountAsync(id);
                if (!result)
                    return NotFound(new { error = "Email template not found" });

                return Ok(new { message = "Failure count incremented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing failure count: {TemplateId}", id);
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// Get unique categories from all email templates
        /// </summary>
        [HttpGet("categories")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
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

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email template categories");
                return StatusCode(500, new { error = "Internal server error occurred" });
            }
        }
    }
}
