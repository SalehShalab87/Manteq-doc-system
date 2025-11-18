using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using EmailService.WebApi.Models;
using CMS.WebApi.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EmailService.WebApi.Services
{
    /// <summary>
    /// Main email sending service interface
    /// </summary>
    public interface IEmailSendingService
    {
        Task<EmailSendResponse> SendEmailWithTemplateAsync(SendEmailWithTemplateRequest request);
        Task<EmailSendResponse> SendEmailWithDocumentsAsync(SendEmailWithDocumentsRequest request);
        Task<EmailSendResponse> SendEmailWithTmsHtmlAndAttachmentAsync(SendEmailWithTmsHtmlAndAttachmentRequest request);
        Task<EmailSendResponse> TestEmailTemplateAsync(TestEmailTemplateRequest request);
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
        private readonly CMS.WebApi.Services.IEmailTemplateService _emailTemplateService;
        private readonly CMS.WebApi.Services.IEmailTemplateFileService _fileService;

        public EmailSendingService(
            IConfiguration configuration,
            ILogger<EmailSendingService> logger,
            ITmsIntegrationService tmsService,
            ICmsIntegrationService cmsService,
            CMS.WebApi.Services.IEmailTemplateService emailTemplateService,
            CMS.WebApi.Services.IEmailTemplateFileService fileService)
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
            _emailTemplateService = emailTemplateService;
            _fileService = fileService;
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
            // Use a more comprehensive email validation regex that allows dots, hyphens, and underscores
            var emailRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            return emailAddresses.All(email => !string.IsNullOrWhiteSpace(email) && emailRegex.IsMatch(email.Trim()));
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

        public async Task<EmailSendResponse> SendEmailWithTmsHtmlAndAttachmentAsync(SendEmailWithTmsHtmlAndAttachmentRequest request)
        {
            var emailId = Guid.NewGuid();
            _logger.LogInformation("Starting email send with TMS EmailHtml as body and {ExportFormat} as attachment, EmailId: {EmailId}", request.AttachmentExportFormat, emailId);

            try
            {
                // Validate email addresses
                if (!ValidateEmailAddresses(request.ToRecipients.Concat(request.CcRecipients).Concat(request.BccRecipients)))
                {
                    throw new ArgumentException("One or more email addresses are invalid");
                }

                // Get sender account
                var senderAccount = GetSenderAccount(request.FromAccount);

                // Generate TMS EmailHtml for body
                var bodyDoc = await _tmsService.GenerateDocumentAsync(request.BodyTemplateId, request.BodyPropertyValues, TmsExportFormat.EmailHtml);

                // Generate TMS doc for attachment
                var attachmentDoc = await _tmsService.GenerateDocumentAsync(request.AttachmentTemplateId, request.AttachmentPropertyValues, request.AttachmentExportFormat);

                try
                {
                    // Create email message
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress(senderAccount.DisplayName, senderAccount.EmailAddress));
                    foreach (var to in request.ToRecipients)
                        message.To.Add(MailboxAddress.Parse(to));
                    foreach (var cc in request.CcRecipients)
                        message.Cc.Add(MailboxAddress.Parse(cc));
                    foreach (var bcc in request.BccRecipients)
                        message.Bcc.Add(MailboxAddress.Parse(bcc));
                    message.Subject = request.Subject;

                    var bodyBuilder = new BodyBuilder();
                    bodyBuilder.HtmlBody = bodyDoc.Content;
                    bodyBuilder.Attachments.Add(attachmentDoc.FileName, attachmentDoc.FileContent, ContentType.Parse(GetMimeType(attachmentDoc.FileName)));

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
                    // Clean up generated documents
                    await _tmsService.CleanupGeneratedDocumentAsync(bodyDoc.GenerationId);
                    await _tmsService.CleanupGeneratedDocumentAsync(attachmentDoc.GenerationId);
                    _logger.LogDebug("Cleaned up generated documents: {BodyId}, {AttachmentId}", bodyDoc.GenerationId, attachmentDoc.GenerationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email with TMS html and attachment, EmailId: {EmailId}", emailId);
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

        /// <summary>
        /// Test email template with full support for all body types and attachments
        /// </summary>
        public async Task<EmailSendResponse> TestEmailTemplateAsync(TestEmailTemplateRequest request)
        {
            var emailId = Guid.NewGuid();
            _logger.LogInformation("Testing email template: TemplateId={TemplateId}, EmailId={EmailId}", request.TemplateId, emailId);

            try
            {
                // Get the email template from local database
                var template = await _emailTemplateService.GetEmailTemplateByIdAsync(request.TemplateId);
                if (template == null)
                {
                    throw new ArgumentException($"Email template not found: {request.TemplateId}");
                }

                // Create email message
                var message = new MimeMessage();
                
                // Set sender
                var fromAccount = _emailConfig.Accounts.FirstOrDefault(a => a.IsDefault) 
                    ?? _emailConfig.Accounts.FirstOrDefault();
                
                if (fromAccount == null)
                {
                    throw new InvalidOperationException("No email account configured");
                }

                message.From.Add(new MailboxAddress(fromAccount.DisplayName, fromAccount.EmailAddress));

                // Set recipients
                foreach (var to in request.ToRecipients)
                {
                    message.To.Add(MailboxAddress.Parse(to));
                }

                foreach (var cc in request.CcRecipients)
                {
                    message.Cc.Add(MailboxAddress.Parse(cc));
                }

                foreach (var bcc in request.BccRecipients)
                {
                    message.Bcc.Add(MailboxAddress.Parse(bcc));
                }

                message.Subject = $"[TEST] {template.Subject}";

                // Build email body and attachments based on template configuration
                var builder = new BodyBuilder();

                // Handle body based on BodySourceType
                switch (template.BodySourceType)
                {
                    case CMS.WebApi.Models.EmailBodySourceType.PlainText:
                        builder.TextBody = template.PlainTextContent;
                        builder.HtmlBody = $"<pre>{System.Net.WebUtility.HtmlEncode(template.PlainTextContent)}</pre>";
                        break;

                    case CMS.WebApi.Models.EmailBodySourceType.TmsTemplate:
                        if (template.TmsTemplateId == null)
                        {
                            throw new ArgumentException("TMS template ID is required for TmsTemplate body source");
                        }

                        if (request.TmsBodyPropertyValues == null || request.TmsBodyPropertyValues.Count == 0)
                        {
                            throw new ArgumentException("TmsBodyPropertyValues are required for TmsTemplate body source");
                        }

                        var bodyDoc = await _tmsService.GenerateDocumentAsync(
                            template.TmsTemplateId.Value,
                            request.TmsBodyPropertyValues,
                            TmsExportFormat.EmailHtml);

                        builder.HtmlBody = System.Text.Encoding.UTF8.GetString(bodyDoc.FileContent);
                        break;

                    case CMS.WebApi.Models.EmailBodySourceType.CustomTemplate:
                        if (string.IsNullOrEmpty(template.CustomTemplateFilePath))
                        {
                            throw new ArgumentException("Custom template file path is required for CustomTemplate body source");
                        }

                        // Read custom template file
                        var customHtmlBytes = await _fileService.GetCustomTemplateAsync(template.Id);
                        builder.HtmlBody = System.Text.Encoding.UTF8.GetString(customHtmlBytes);
                        break;

                    default:
                        throw new ArgumentException($"Unsupported body source type: {template.BodySourceType}");
                }

                // Handle attachments from template's default attachments
                var attachments = template.Attachments ?? new List<CMS.WebApi.Models.EmailTemplateAttachmentResponse>();
                int attachmentIndex = 0;

                foreach (var attachment in attachments)
                {
                    try
                    {
                        switch (attachment.SourceType)
                        {
                            case CMS.WebApi.Models.AttachmentSourceType.CmsDocument:
                                if (attachment.CmsDocumentId.HasValue)
                                {
                                    var doc = await _cmsService.GetDocumentAsync(attachment.CmsDocumentId.Value);
                                    if (doc != null)
                                    {
                                        builder.Attachments.Add(attachment.CustomFileName ?? doc.FileName, doc.FileContent);
                                    }
                                }
                                break;

                            case CMS.WebApi.Models.AttachmentSourceType.TmsTemplate:
                                if (attachment.TmsTemplateId.HasValue)
                                {
                                    if (request.TmsAttachmentPropertyValues == null || 
                                        !request.TmsAttachmentPropertyValues.ContainsKey(attachmentIndex))
                                    {
                                        throw new ArgumentException($"TmsAttachmentPropertyValues are required for TMS attachment at index {attachmentIndex}");
                                    }

                                    var exportFormat = attachment.TmsExportFormat.HasValue 
                                        ? (TmsExportFormat)attachment.TmsExportFormat.Value 
                                        : TmsExportFormat.Pdf;
                                    var tmsDoc = await _tmsService.GenerateDocumentAsync(
                                        attachment.TmsTemplateId.Value,
                                        request.TmsAttachmentPropertyValues[attachmentIndex],
                                        exportFormat);

                                    var extension = exportFormat switch
                                    {
                                        TmsExportFormat.Pdf => "pdf",
                                        TmsExportFormat.Word => "docx",
                                        TmsExportFormat.Html => "html",
                                        _ => "pdf"
                                    };

                                    builder.Attachments.Add(attachment.CustomFileName ?? $"attachment{attachmentIndex}.{extension}", tmsDoc.FileContent);
                                }
                                break;

                            case CMS.WebApi.Models.AttachmentSourceType.CustomFile:
                                if (!string.IsNullOrEmpty(attachment.CustomFilePath))
                                {
                                    var (fileBytes, fileName, contentType) = await _fileService.GetCustomAttachmentAsync(template.Id, attachmentIndex);
                                    builder.Attachments.Add(attachment.CustomFileName ?? fileName, fileBytes);
                                }
                                break;
                        }

                        attachmentIndex++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to attach file at index {Index}: {FileName}", attachmentIndex, attachment.CustomFileName);
                        // Continue with other attachments
                    }
                }

                message.Body = builder.ToMessageBody();

                // Send the email
                using (var smtp = new SmtpClient())
                {
                    await smtp.ConnectAsync(_smtpConfig.Host, _smtpConfig.Port, _smtpConfig.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
                    await smtp.AuthenticateAsync(_smtpConfig.Username, _smtpConfig.Password);
                    await smtp.SendAsync(message);
                    await smtp.DisconnectAsync(true);
                }

                _logger.LogInformation("Test email sent successfully: {EmailId}", emailId);

                return new EmailSendResponse
                {
                    EmailId = emailId,
                    Message = "Test email sent successfully",
                    Status = EmailStatus.Sent,
                    SentAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test email: TemplateId={TemplateId}, EmailId={EmailId}", request.TemplateId, emailId);
                return new EmailSendResponse
                {
                    EmailId = emailId,
                    Message = "Failed to send test email",
                    Status = EmailStatus.Failed,
                    SentAt = DateTime.UtcNow,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
