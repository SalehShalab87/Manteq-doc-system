using System.ComponentModel.DataAnnotations;

namespace CMS.WebApi.Models
{
    /// <summary>
    /// Represents a default attachment associated with an email template
    /// </summary>
    public class EmailTemplateAttachment
    {
        /// <summary>
        /// Unique identifier for the attachment
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Reference to the parent email template
        /// </summary>
        [Required]
        public Guid EmailTemplateId { get; set; }

        /// <summary>
        /// Navigation property to parent email template
        /// </summary>
        public EmailTemplate EmailTemplate { get; set; } = null!;

        /// <summary>
        /// Source type of the attachment (CMS, TMS, or Custom)
        /// </summary>
        [Required]
        public AttachmentSourceType SourceType { get; set; }

        /// <summary>
        /// CMS Document ID (if SourceType is CmsDocument)
        /// </summary>
        public Guid? CmsDocumentId { get; set; }

        /// <summary>
        /// Navigation property to CMS document
        /// </summary>
        public Document? CmsDocument { get; set; }

        /// <summary>
        /// TMS Template ID (if SourceType is TmsTemplate)
        /// </summary>
        public Guid? TmsTemplateId { get; set; }

        /// <summary>
        /// TMS export format for template generation
        /// </summary>
        public int? TmsExportFormat { get; set; }

        /// <summary>
        /// File path to custom uploaded file (if SourceType is CustomFile)
        /// </summary>
        [StringLength(500)]
        public string? CustomFilePath { get; set; }

        /// <summary>
        /// Original file name for custom uploaded files
        /// </summary>
        [StringLength(255)]
        public string? CustomFileName { get; set; }

        /// <summary>
        /// File size in bytes (for custom files)
        /// </summary>
        public long? FileSize { get; set; }

        /// <summary>
        /// MIME type of the file (for custom files)
        /// </summary>
        [StringLength(100)]
        public string? MimeType { get; set; }

        /// <summary>
        /// Display order for attachments (lower numbers appear first)
        /// </summary>
        [Required]
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Date and time when the attachment was added
        /// </summary>
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who added this attachment
        /// </summary>
        [Required]
        [StringLength(100)]
        public string CreatedBy { get; set; } = "SYSTEM";
    }
}
