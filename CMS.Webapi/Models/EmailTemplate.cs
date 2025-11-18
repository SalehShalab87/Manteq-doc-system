using System.ComponentModel.DataAnnotations;

namespace CMS.WebApi.Models
{
    /// <summary>
    /// Defines how the email body content is sourced
    /// </summary>
    public enum EmailBodySourceType
    {
        /// <summary>
        /// Plain text only (no HTML formatting)
        /// </summary>
        PlainText = 1,
        
        /// <summary>
        /// Generated from a TMS template (EmailHtml format)
        /// </summary>
        TmsTemplate = 2,
        
        /// <summary>
        /// Custom uploaded HTML/XHTML template file
        /// </summary>
        CustomTemplate = 3
    }

    /// <summary>
    /// Defines the source type for email attachments
    /// </summary>
    public enum AttachmentSourceType
    {
        /// <summary>
        /// Existing CMS document
        /// </summary>
        CmsDocument = 1,
        
        /// <summary>
        /// TMS-generated document (created at send time)
        /// </summary>
        TmsTemplate = 2,
        
        /// <summary>
        /// Custom uploaded file stored in shared storage
        /// </summary>
        CustomFile = 3
    }

    /// <summary>
    /// Export format for TMS-generated documents
    /// </summary>
    public enum TmsExportFormat
    {
        Pdf = 0,
        Word = 1,
        Html = 2,
        EmailHtml = 3,
        Excel = 4
    }

    /// <summary>
    /// Email template entity for storing email-specific templates with HTML content
    /// Supports tracking of sent/failed emails and optional linkage to document templates
    /// Attachments can be from CMS documents and/or TMS templates - specified per email send
    /// </summary>
    public class EmailTemplate
    {
        /// <summary>
        /// Unique identifier for the email template
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the email template (e.g., "Welcome Email", "Invoice Notification")
        /// </summary>
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Email subject line (supports placeholders)
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// HTML content of the email (supports placeholders)
        /// Not required when using PlainText or CustomTemplate body source types
        /// </summary>
        public string? HtmlContent { get; set; }

        /// <summary>
        /// Plain text version of the email (fallback for non-HTML clients)
        /// </summary>
        public string? PlainTextContent { get; set; }

        /// <summary>
        /// Optional reference to a document template (if email is generated from a template)
        /// </summary>
        public Guid? TemplateId { get; set; }

        /// <summary>
        /// Navigation property to related Template
        /// </summary>
        public Template? Template { get; set; }

        /// <summary>
        /// Defines how the email body content is sourced (PlainText, TMS Template, or Custom Template)
        /// </summary>
        [Required]
        public EmailBodySourceType BodySourceType { get; set; } = EmailBodySourceType.PlainText;

        /// <summary>
        /// TMS Template ID to use for generating the email body (when BodySourceType is TmsTemplate)
        /// </summary>
        public Guid? TmsTemplateId { get; set; }

        /// <summary>
        /// File path to custom uploaded HTML/XHTML template (when BodySourceType is CustomTemplate)
        /// </summary>
        [StringLength(500)]
        public string? CustomTemplateFilePath { get; set; }

        /// <summary>
        /// Navigation property for default attachments
        /// </summary>
        public ICollection<EmailTemplateAttachment> DefaultAttachments { get; set; } = new List<EmailTemplateAttachment>();

        /// <summary>
        /// Whether this email template is active and available for use
        /// </summary>
        [Required]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Soft delete flag
        /// </summary>
        public bool IsDeleted { get; set; } = false;
        
        /// <summary>
        /// Date and time when the email template was deleted
        /// </summary>
        public DateTime? DeletedAt { get; set; }
        
        /// <summary>
        /// User who deleted this email template
        /// </summary>
        [StringLength(100)]
        public string? DeletedBy { get; set; }

        /// <summary>
        /// Category/type of email (e.g., "Transactional", "Marketing", "Notification")
        /// </summary>
        [StringLength(100)]
        public string? Category { get; set; }

        /// <summary>
        /// Number of times this template was successfully sent
        /// </summary>
        [Required]
        public int SentCount { get; set; } = 0;

        /// <summary>
        /// Number of times sending this template failed
        /// </summary>
        [Required]
        public int FailureCount { get; set; } = 0;

        /// <summary>
        /// User who created this email template (from X-SME-UserId header)
        /// </summary>
        [Required]
        [StringLength(100)]
        public string CreatedBy { get; set; } = "SYSTEM";

        /// <summary>
        /// Date and time when the email template was created
        /// </summary>
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
