using Microsoft.AspNetCore.Mvc;
using EmailService.WebApi.Models;
using EmailService.WebApi.Services;
using System.ComponentModel.DataAnnotations;

namespace EmailService.WebApi.Controllers
{
    /// <summary>
    /// Email Service API Controller for sending emails with TMS templates and CMS documents
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailSendingService _emailService;
        private readonly ILogger<EmailController> _logger;

        public EmailController(IEmailSendingService emailService, ILogger<EmailController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Send email with TMS-generated template content
        /// </summary>
        /// <remarks>
        /// This endpoint generates a document from a TMS template and sends it via email.
        /// 
        /// **Special Behavior for EmailHtml format:**
        /// - EmailHtml export format will replace the email body content (no attachment)
        /// - Other formats will attach the generated document to the email
        /// 
        /// **Example Request:**
        /// ```json
        /// {
        ///   "toRecipients": ["user@example.com"],
        ///   "subject": "Your Generated Document",
        ///   "templateId": "550e8400-e29b-41d4-a716-446655440000",
        ///   "propertyValues": {
        ///     "CustomerName": "John Doe",
        ///     "InvoiceNumber": "INV-2023-001"
        ///   },
        ///   "exportFormat": "EmailHtml"
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Email request with template information</param>
        /// <returns>Email send response with status</returns>
        [HttpPost("send-with-template")]
        [ProducesResponseType(typeof(EmailSendResponse), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SendEmailWithTemplate([FromBody] SendEmailWithTemplateRequest request)
        {
            _logger.LogInformation("Received request to send email with template: {TemplateId} to {RecipientCount} recipients", 
                request.TemplateId, request.ToRecipients.Count);

            try
            {
                var response = await _emailService.SendEmailWithTemplateAsync(request);
                
                if (response.Status == EmailStatus.Sent)
                {
                    _logger.LogInformation("Email sent successfully: {EmailId}", response.EmailId);
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning("Email sending failed: {EmailId}, Error: {Error}", response.EmailId, response.ErrorMessage);
                    return StatusCode(500, response);
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for send-with-template: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending email with template: {TemplateId}", request.TemplateId);
                return StatusCode(500, new { error = "An unexpected error occurred while sending the email" });
            }
        }

        /// <summary>
        /// Send email with CMS document attachments
        /// </summary>
        /// <remarks>
        /// This endpoint sends an email with documents from the CMS attached.
        /// 
        /// **Example Request:**
        /// ```json
        /// {
        ///   "toRecipients": ["user@example.com"],
        ///   "subject": "Documents Attached",
        ///   "htmlBody": "&lt;p&gt;Please find the attached documents.&lt;/p&gt;",
        ///   "cmsDocumentIds": [
        ///     "550e8400-e29b-41d4-a716-446655440000",
        ///     "550e8400-e29b-41d4-a716-446655440001"
        ///   ]
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Email request with document attachments</param>
        /// <returns>Email send response with status</returns>
        [HttpPost("send-with-documents")]
        [ProducesResponseType(typeof(EmailSendResponse), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SendEmailWithDocuments([FromBody] SendEmailWithDocumentsRequest request)
        {
            _logger.LogInformation("Received request to send email with {DocumentCount} CMS documents to {RecipientCount} recipients", 
                request.CmsDocumentIds.Count, request.ToRecipients.Count);

            try
            {
                var response = await _emailService.SendEmailWithDocumentsAsync(request);
                
                if (response.Status == EmailStatus.Sent)
                {
                    _logger.LogInformation("Email sent successfully: {EmailId}", response.EmailId);
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning("Email sending failed: {EmailId}, Error: {Error}", response.EmailId, response.ErrorMessage);
                    return StatusCode(500, response);
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for send-with-documents: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending email with documents");
                return StatusCode(500, new { error = "An unexpected error occurred while sending the email" });
            }
        }

        /// <summary>
        /// Send email with TMS EmailHtml as body and another TMS export as attachment
        /// </summary>
        /// <remarks>
        /// This endpoint generates a document from a TMS template twice:
        /// - Once as EmailHtml (for the email body)
        /// - Once as the requested export format (for the attachment)
        /// </remarks>
        /// <param name="request">Email request with template info and attachment export format</param>
        /// <returns>Email send response with status</returns>
        [HttpPost("send-tms-html-and-attachment")]
        [ProducesResponseType(typeof(EmailSendResponse), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SendTmsHtmlAndAttachment([FromBody] SendEmailWithTmsHtmlAndAttachmentRequest request)
        {
            _logger.LogInformation("Received request to send email with TMS EmailHtml as body (BodyTemplate={BodyTemplateId}) and {ExportFormat} as attachment (AttachmentTemplate={AttachmentTemplateId})",
                request.AttachmentExportFormat, request.BodyTemplateId, request.AttachmentTemplateId);

            try
            {
                var response = await _emailService.SendEmailWithTmsHtmlAndAttachmentAsync(request);
                if (response.Status == EmailStatus.Sent)
                {
                    _logger.LogInformation("Email sent successfully: {EmailId}", response.EmailId);
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning("Email sending failed: {EmailId}, Error: {Error}", response.EmailId, response.ErrorMessage);
                    return StatusCode(500, response);
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for send-tms-html-and-attachment: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending email with TMS html and attachment: BodyTemplate={BodyTemplateId}, AttachmentTemplate={AttachmentTemplateId}", request.BodyTemplateId, request.AttachmentTemplateId);
                return StatusCode(500, new { error = "An unexpected error occurred while sending the email" });
            }
        }

        /// <summary>
        /// Get available email accounts
        /// </summary>
        /// <returns>List of configured email accounts</returns>
        [HttpGet("accounts")]
        [ProducesResponseType(typeof(List<EmailAccount>), 200)]
        public IActionResult GetEmailAccounts()
        {
            try
            {
                var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                var emailConfig = config.GetSection("Email").Get<EmailConfiguration>();
                
                if (emailConfig?.Accounts == null || emailConfig.Accounts.Count == 0)
                {
                    return Ok(new List<EmailAccount>());
                }

                // Return accounts without sensitive information
                var sanitizedAccounts = emailConfig.Accounts.Select(a => new EmailAccount
                {
                    Name = a.Name,
                    DisplayName = a.DisplayName,
                    EmailAddress = a.EmailAddress,
                    IsDefault = a.IsDefault
                }).ToList();

                return Ok(sanitizedAccounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email accounts");
                return StatusCode(500, new { error = "Unable to retrieve email accounts" });
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        /// <returns>Service status</returns>
        [HttpGet("health")]
        [ProducesResponseType(200)]
        public IActionResult HealthCheck()
        {
            return Ok(new { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                service = "EmailService.WebApi"
            });
        }

        /// <summary>
        /// Test an email template with full support for all body types and attachments
        /// </summary>
        /// <remarks>
        /// This endpoint tests email templates with support for:
        /// - Plain text body
        /// - TMS-generated HTML body
        /// - Custom HTML template body
        /// - Multiple attachments (CMS documents, TMS-generated files, custom files)
        /// 
        /// For TMS body generation, provide TmsBodyPropertyValues.
        /// For TMS attachments, provide TmsAttachmentPropertyValues indexed by attachment position.
        /// </remarks>
        /// <param name="request">Test email request</param>
        /// <returns>Email send response</returns>
        [HttpPost("test-template")]
        [ProducesResponseType(typeof(EmailSendResponse), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> TestEmailTemplate([FromBody] TestEmailTemplateRequest request)
        {
            _logger.LogInformation("Testing email template: {TemplateId} to {RecipientCount} recipients", 
                request.TemplateId, request.ToRecipients.Count);

            try
            {
                var response = await _emailService.TestEmailTemplateAsync(request);
                
                if (response.Status == EmailStatus.Sent)
                {
                    _logger.LogInformation("Test email sent successfully: {EmailId}", response.EmailId);
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning("Test email failed: {EmailId}, Error: {Error}", response.EmailId, response.ErrorMessage);
                    return StatusCode(500, response);
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for test-template: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error testing email template: {TemplateId}", request.TemplateId);
                return StatusCode(500, new { error = "An unexpected error occurred while testing the email template" });
            }
        }
    }
}