using System.ComponentModel.DataAnnotations;

namespace CMS.WebApi.Models
{
    /// <summary>
    /// Email template entity for storing email-specific templates with HTML content
    /// Supports tracking of sent/failed emails and optional linkage to document templates
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
        /// </summary>
        [Required]
        public string HtmlContent { get; set; } = string.Empty;

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
        /// Whether this email template is active and available for use
        /// </summary>
        [Required]
        public bool IsActive { get; set; } = true;

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
