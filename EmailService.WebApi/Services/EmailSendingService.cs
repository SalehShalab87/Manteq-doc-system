using EmailService.WebApi.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EmailService.WebApi.Services
{
    /// <summary>
    /// Main email sending service interface
    /// </summary>
    public interface IEmailSendingService
    {
        Task<EmailSendResponse> SendEmailWithTemplateAsync(SendEmailWithTemplateRequest request);
        Task<EmailSendResponse> SendEmailWithDocumentsAsync(SendEmailWithDocumentsRequest request);
    }

    /// <summary>
    /// Implementation of email sending service using MailKit/MimeKit
    /// </summary>
    public class EmailSendingService : IEmailSendingService
    {
        private readonly EmailConfiguration _emailConfig;
        private readonly SmtpConfiguration _smtpConfig;
        private readonly ILogger<EmailSendingService> _logger;
        private readonly ITmsIntegrationService _tmsService;
        private readonly ICmsIntegrationService _cmsService;

        public EmailSendingService(
            IConfiguration configuration,
            ILogger<EmailSendingService> logger,
            ITmsIntegrationService tmsService,
            ICmsIntegrationService cmsService)
        {
            _emailConfig = configuration.GetSection("Email").Get<EmailConfiguration>() ?? new EmailConfiguration();
            _smtpConfig = new SmtpConfiguration
            {
                Host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? configuration["Email:Smtp:Host"] ?? "",
                Port = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? configuration["Email:Smtp:Port"] ?? "587"),
                EnableSsl = bool.Parse(Environment.GetEnvironmentVariable("SMTP_SSL") ?? configuration["Email:Smtp:EnableSsl"] ?? "true"),
                Username = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? configuration["Email:Smtp:Username"] ?? "",
                Password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? configuration["Email:Smtp:Password"] ?? ""
            };
            _logger = logger;
            _tmsService = tmsService;
            _cmsService = cmsService;
        }

        public async Task<EmailSendResponse> SendEmailWithTemplateAsync(SendEmailWithTemplateRequest request)
        {
            var emailId = Guid.NewGuid();
            _logger.LogInformation("Starting email send with template: {TemplateId}, EmailId: {EmailId}", request.TemplateId, emailId);

            try
            {
                // Validate email addresses
                if (!ValidateEmailAddresses(request.ToRecipients.Concat(request.CcRecipients).Concat(request.BccRecipients)))
                {
                    throw new ArgumentException("One or more email addresses are invalid");
                }

                // Get sender account
                var senderAccount = GetSenderAccount(request.FromAccount);

                // Generate document from TMS
                var generatedDoc = await _tmsService.GenerateDocumentAsync(request.TemplateId, request.PropertyValues, request.ExportFormat);

                try
                {
                    // Create email message
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress(senderAccount.DisplayName, senderAccount.EmailAddress));
                    
                    // Add recipients
                    foreach (var to in request.ToRecipients)
                        message.To.Add(MailboxAddress.Parse(to));
                    foreach (var cc in request.CcRecipients)
                        message.Cc.Add(MailboxAddress.Parse(cc));
                    foreach (var bcc in request.BccRecipients)
                        message.Bcc.Add(MailboxAddress.Parse(bcc));

                    message.Subject = request.Subject;

                    var bodyBuilder = new BodyBuilder();

                    // Handle EmailHtml format - replace body content
                    if (request.ExportFormat == TmsExportFormat.EmailHtml)
                    {
                        bodyBuilder.HtmlBody = generatedDoc.Content;
                        _logger.LogInformation("Using generated EmailHtml as email body");
                    }
                    else
                    {
                        // Use provided body content and attach generated document
                        if (!string.IsNullOrEmpty(request.HtmlBody))
                            bodyBuilder.HtmlBody = request.HtmlBody;
                        if (!string.IsNullOrEmpty(request.PlainTextBody))
                            bodyBuilder.TextBody = request.PlainTextBody;

                        // Add generated document as attachment
                        bodyBuilder.Attachments.Add(generatedDoc.FileName, generatedDoc.FileContent, ContentType.Parse(GetMimeType(generatedDoc.FileName)));
                        _logger.LogInformation("Added generated document as attachment: {FileName}", generatedDoc.FileName);
                    }

                    message.Body = bodyBuilder.ToMessageBody();

                    // Send email
                    await SendEmailAsync(message);

                    _logger.LogInformation("Email sent successfully: {EmailId}", emailId);

                    return new EmailSendResponse
                    {
                        EmailId = emailId,
                        Message = "Email sent successfully",
                        Status = EmailStatus.Sent,
                        SentAt = DateTime.UtcNow
                    };
                }
                finally
                {
                    // Clean up generated document
                    await _tmsService.CleanupGeneratedDocumentAsync(generatedDoc.GenerationId);
                    _logger.LogDebug("Cleaned up generated document: {GenerationId}", generatedDoc.GenerationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email with template: {TemplateId}, EmailId: {EmailId}", request.TemplateId, emailId);
                return new EmailSendResponse
                {
                    EmailId = emailId,
                    Message = "Failed to send email",
                    Status = EmailStatus.Failed,
                    SentAt = DateTime.UtcNow,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<EmailSendResponse> SendEmailWithDocumentsAsync(SendEmailWithDocumentsRequest request)
        {
            var emailId = Guid.NewGuid();
            _logger.LogInformation("Starting email send with documents, EmailId: {EmailId}", emailId);

            try
            {
                // Validate email addresses
                if (!ValidateEmailAddresses(request.ToRecipients.Concat(request.CcRecipients).Concat(request.BccRecipients)))
                {
                    throw new ArgumentException("One or more email addresses are invalid");
                }

                // Get sender account
                var senderAccount = GetSenderAccount(request.FromAccount);

                // Create email message
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderAccount.DisplayName, senderAccount.EmailAddress));
                
                // Add recipients
                foreach (var to in request.ToRecipients)
                    message.To.Add(MailboxAddress.Parse(to));
                foreach (var cc in request.CcRecipients)
                    message.Cc.Add(MailboxAddress.Parse(cc));
                foreach (var bcc in request.BccRecipients)
                    message.Bcc.Add(MailboxAddress.Parse(bcc));

                message.Subject = request.Subject;

                var bodyBuilder = new BodyBuilder();

                // Set email body
                if (!string.IsNullOrEmpty(request.HtmlBody))
                    bodyBuilder.HtmlBody = request.HtmlBody;
                if (!string.IsNullOrEmpty(request.PlainTextBody))
                    bodyBuilder.TextBody = request.PlainTextBody;

                // Add CMS document attachments
                foreach (var documentId in request.CmsDocumentIds)
                {
                    var document = await _cmsService.GetDocumentAsync(documentId);
                    if (document != null)
                    {
                        bodyBuilder.Attachments.Add(document.FileName, document.FileContent, ContentType.Parse(document.ContentType));
                        _logger.LogInformation("Added CMS document attachment: {FileName}", document.FileName);
                    }
                    else
                    {
                        _logger.LogWarning("CMS document not found: {DocumentId}", documentId);
                    }
                }

                message.Body = bodyBuilder.ToMessageBody();

                // Send email
                await SendEmailAsync(message);

                _logger.LogInformation("Email sent successfully with documents: {EmailId}", emailId);

                return new EmailSendResponse
                {
                    EmailId = emailId,
                    Message = "Email sent successfully",
                    Status = EmailStatus.Sent,
                    SentAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email with documents, EmailId: {EmailId}", emailId);
                return new EmailSendResponse
                {
                    EmailId = emailId,
                    Message = "Failed to send email",
                    Status = EmailStatus.Failed,
                    SentAt = DateTime.UtcNow,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task SendEmailAsync(MimeMessage message)
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpConfig.Host, _smtpConfig.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtpConfig.Username, _smtpConfig.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        private EmailAccount GetSenderAccount(string? accountName)
        {
            if (string.IsNullOrEmpty(accountName))
            {
                var defaultAccount = _emailConfig.Accounts.FirstOrDefault(a => a.IsDefault);
                if (defaultAccount == null)
                    throw new InvalidOperationException("No default email account configured");
                return defaultAccount;
            }

            var account = _emailConfig.Accounts.FirstOrDefault(a => a.Name.Equals(accountName, StringComparison.OrdinalIgnoreCase));
            if (account == null)
                throw new ArgumentException($"Email account '{accountName}' not found");

            return account;
        }

        private static bool ValidateEmailAddresses(IEnumerable<string> emailAddresses)
        {
            var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return emailAddresses.All(email => emailRegex.IsMatch(email));
        }

        private static string GetMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".doc" => "application/msword",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xls" => "application/vnd.ms-excel",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".html" => "text/html",
                ".htm" => "text/html",
                ".txt" => "text/plain",
                ".rtf" => "application/rtf",
                _ => "application/octet-stream"
            };
        }
    }
}
