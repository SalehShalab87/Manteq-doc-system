using System.ComponentModel.DataAnnotations;

namespace EmailService.WebApi.Models
{
    /// <summary>
    /// Flexible email sending request that supports mixed CMS and TMS attachments
    /// </summary>
    public class SendEmailWithFlexibleAttachmentsRequest
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
        /// Email template ID from CMS (contains body configuration)
        /// </summary>
        [Required]
        public Guid EmailTemplateId { get; set; }
        
        /// <summary>
        /// Property values for TMS body template (if template uses TMS for body)
        /// </summary>
        public Dictionary<string, string>? BodyPropertyValues { get; set; }
        
        /// <summary>
        /// List of CMS document IDs to attach
        /// </summary>
        public List<Guid> CmsDocumentIds { get; set; } = new();
        
        /// <summary>
        /// List of TMS templates to generate and attach
        /// </summary>
        public List<TmsAttachmentRequest> TmsAttachments { get; set; } = new();
    }

    /// <summary>
    /// Request to generate a TMS template as an attachment
    /// </summary>
    public class TmsAttachmentRequest
    {
        /// <summary>
        /// TMS template ID to generate
        /// </summary>
        [Required]
        public Guid TemplateId { get; set; }
        
        /// <summary>
        /// Export format for the attachment
        /// </summary>
        [Required]
        public TmsExportFormat ExportFormat { get; set; }
        
        /// <summary>
        /// Property values to inject into the template
        /// </summary>
        [Required]
        public Dictionary<string, string> PropertyValues { get; set; } = new();
        
        /// <summary>
        /// Optional custom filename for the attachment
        /// </summary>
        public string? CustomFilename { get; set; }
    }
}
