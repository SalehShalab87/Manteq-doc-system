
using System.ComponentModel.DataAnnotations;


namespace EmailService.WebApi.Models
{
    /// <summary>
    /// Request to send email with TMS EmailHtml as body and another TMS export as attachment, each with its own property values
    /// </summary>
    public class SendEmailWithTmsHtmlAndAttachmentRequest
    {
        public string? FromAccount { get; set; }
        [Required]
        public List<string> ToRecipients { get; set; } = new();
        public List<string> CcRecipients { get; set; } = new();
        public List<string> BccRecipients { get; set; } = new();
        [Required]
        public string Subject { get; set; } = string.Empty;
        /// <summary>
        /// TMS template ID for the email body (EmailHtml)
        /// </summary>
        [Required]
        public Guid BodyTemplateId { get; set; }
        /// <summary>
        /// Property values for the email body (EmailHtml)
        /// </summary>
        [Required]
        public Dictionary<string, string> BodyPropertyValues { get; set; } = new();
        /// <summary>
        /// TMS template ID for the attachment (can be the same or different from body)
        /// </summary>
        [Required]
        public Guid AttachmentTemplateId { get; set; }
        /// <summary>
        /// Property values for the attachment
        /// </summary>
        [Required]
        public Dictionary<string, string> AttachmentPropertyValues { get; set; } = new();
        /// <summary>
        /// Export format for the attachment (not EmailHtml)
        /// </summary>
        [Required]
        public TmsExportFormat AttachmentExportFormat { get; set; }
    }
    public class EmailAccount
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public bool IsDefault { get; set; } = false;
    }

    public class SmtpConfiguration
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class EmailConfiguration
    {
        public SmtpConfiguration Smtp { get; set; } = new();
        public List<EmailAccount> Accounts { get; set; } = new();
    }

    public enum EmailStatus
    {
        Pending = 0,
        Sent = 1,
        Failed = 2
    }

    public enum TmsExportFormat
    {
        Original = 0,
        Word = 1,
        Html = 2,
        EmailHtml = 3,
        Pdf = 4
    }

    public class SendEmailWithTemplateRequest
    {
        /// <summary>
        /// The email account to send from (optional - uses default if not specified)
        /// </summary>
        public string? FromAccount { get; set; }
        
        /// <summary>
        /// List of primary recipients
        /// </summary>
        [Required]
        public List<string> ToRecipients { get; set; } = new();
        
        /// <summary>
        /// List of CC recipients (optional)
        /// </summary>
        public List<string> CcRecipients { get; set; } = new();
        
        /// <summary>
        /// List of BCC recipients (optional)
        /// </summary>
        public List<string> BccRecipients { get; set; } = new();
        
        /// <summary>
        /// Email subject
        /// </summary>
        [Required]
        public string Subject { get; set; } = string.Empty;
        
        /// <summary>
        /// Plain text email body (optional)
        /// </summary>
        public string? PlainTextBody { get; set; }
        
        /// <summary>
        /// HTML email body (optional, ignored if ExportFormat is EmailHtml)
        /// </summary>
        public string? HtmlBody { get; set; }
        
        /// <summary>
        /// TMS template ID to generate content from
        /// </summary>
        [Required]
        public Guid TemplateId { get; set; }
        
        /// <summary>
        /// Property values to inject into the template
        /// </summary>
        [Required]
        public Dictionary<string, string> PropertyValues { get; set; } = new();
        
        /// <summary>
        /// Export format for the generated document
        /// EmailHtml = replaces email body, others = attachment
        /// </summary>
        [Required]
        public TmsExportFormat ExportFormat { get; set; }
    }

    public class SendEmailWithDocumentsRequest
    {
        /// <summary>
        /// The email account to send from (optional - uses default if not specified)
        /// </summary>
        public string? FromAccount { get; set; }
        
        /// <summary>
        /// List of primary recipients
        /// </summary>
        [Required]
        public List<string> ToRecipients { get; set; } = new();
        
        /// <summary>
        /// List of CC recipients (optional)
        /// </summary>
        public List<string> CcRecipients { get; set; } = new();
        
        /// <summary>
        /// List of BCC recipients (optional)
        /// </summary>
        public List<string> BccRecipients { get; set; } = new();
        
        /// <summary>
        /// Email subject
        /// </summary>
        [Required]
        public string Subject { get; set; } = string.Empty;
        
        /// <summary>
        /// Plain text email body (optional)
        /// </summary>
        public string? PlainTextBody { get; set; }
        
        /// <summary>
        /// HTML email body (optional)
        /// </summary>
        public string? HtmlBody { get; set; }
        
        /// <summary>
        /// List of CMS document IDs to attach
        /// </summary>
        public List<Guid> CmsDocumentIds { get; set; } = new();
    }

    public class EmailSendResponse
    {
        /// <summary>
        /// Unique identifier for this email send operation
        /// </summary>
        public Guid EmailId { get; set; }
        
        /// <summary>
        /// Success or error message
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Current status of the email
        /// </summary>
        public EmailStatus Status { get; set; }
        
        /// <summary>
        /// Timestamp when the email was processed
        /// </summary>
        public DateTime SentAt { get; set; }
        
        /// <summary>
        /// Detailed error message if sending failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Request to test an email template with full support for all body types and attachments
    /// </summary>
    public class TestEmailTemplateRequest
    {
        [Required]
        public Guid TemplateId { get; set; }

        [Required]
        public List<string> ToRecipients { get; set; } = new();
        
        public List<string> CcRecipients { get; set; } = new();
        public List<string> BccRecipients { get; set; } = new();

        /// <summary>
        /// Property values for TMS body generation (if template uses TMS for body)
        /// </summary>
        public Dictionary<string, string>? TmsBodyPropertyValues { get; set; }

        /// <summary>
        /// Property values for each TMS attachment by attachment index
        /// Key: attachment index (0-based), Value: property values for that TMS template
        /// </summary>
        public Dictionary<int, Dictionary<string, string>>? TmsAttachmentPropertyValues { get; set; }
    }
}
