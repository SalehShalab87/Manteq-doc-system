using System.ComponentModel.DataAnnotations;

namespace CMS.WebApi.Models
{
    /// <summary>
    /// Request to create a new email template
    /// </summary>
    public class CreateEmailTemplateRequest : IValidatableObject
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Subject { get; set; } = string.Empty;

        public string? HtmlContent { get; set; }

        public string? PlainTextContent { get; set; }

        public Guid? TemplateId { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        /// <summary>
        /// How the email body will be sourced (PlainText, TMS Template, or Custom Template)
        /// </summary>
        [Required]
        public EmailBodySourceType BodySourceType { get; set; } = EmailBodySourceType.PlainText;

        /// <summary>
        /// TMS Template ID for generating email body (required if BodySourceType is TmsTemplate)
        /// </summary>
        public Guid? TmsTemplateId { get; set; }

        /// <summary>
        /// File path to custom template (set by server after upload, when BodySourceType is CustomTemplate)
        /// </summary>
        public string? CustomTemplateFilePath { get; set; }

        /// <summary>
        /// Default attachments for this template
        /// </summary>
        public List<CreateEmailTemplateAttachmentRequest>? Attachments { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (BodySourceType == EmailBodySourceType.PlainText && string.IsNullOrWhiteSpace(PlainTextContent))
            {
                yield return new ValidationResult(
                    "PlainTextContent is required when BodySourceType is PlainText.",
                    new[] { nameof(PlainTextContent) });
            }

            if (BodySourceType == EmailBodySourceType.TmsTemplate && !TmsTemplateId.HasValue)
            {
                yield return new ValidationResult(
                    "TmsTemplateId is required when BodySourceType is TmsTemplate.",
                    new[] { nameof(TmsTemplateId) });
            }

            // Note: CustomTemplate validation happens after file upload
        }
    }

    /// <summary>
    /// Request to update an existing email template
    /// </summary>
    public class UpdateEmailTemplateRequest
    {
        [StringLength(255)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Subject { get; set; }

        public string? HtmlContent { get; set; }

        public string? PlainTextContent { get; set; }

        public Guid? TemplateId { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        public EmailBodySourceType? BodySourceType { get; set; }

        public Guid? TmsTemplateId { get; set; }

        public string? CustomTemplateFilePath { get; set; }

        /// <summary>
        /// Default attachments for this template (replaces existing attachments if provided)
        /// </summary>
        public List<CreateEmailTemplateAttachmentRequest>? Attachments { get; set; }
    }

    /// <summary>
    /// Response containing email template details
    /// </summary>
    public class EmailTemplateResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? HtmlContent { get; set; }
        public string? PlainTextContent { get; set; }
        public Guid? TemplateId { get; set; }
        public EmailBodySourceType BodySourceType { get; set; }
        public Guid? TmsTemplateId { get; set; }
        public string? CustomTemplateFilePath { get; set; }
        public bool IsActive { get; set; }
        public string? Category { get; set; }
        public int SentCount { get; set; }
        public int FailureCount { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public List<EmailTemplateAttachmentResponse>? Attachments { get; set; }

        // Statistics
        public int TotalAttempts => SentCount + FailureCount;
        public double SuccessRate => TotalAttempts > 0 ? (double)SentCount / TotalAttempts * 100 : 0;
    }

    /// <summary>
    /// Response containing email template analytics
    /// </summary>
    public class EmailTemplateAnalyticsResponse
    {
        public Guid TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public int SentCount { get; set; }
        public int FailureCount { get; set; }
        public int TotalAttempts { get; set; }
        public double SuccessRate { get; set; }
    }

    /// <summary>
    /// Request to add attachment to email template
    /// </summary>
    public class CreateEmailTemplateAttachmentRequest
    {
        [Required]
        public AttachmentSourceType SourceType { get; set; }

        /// <summary>
        /// CMS Document ID (required if SourceType is CmsDocument)
        /// </summary>
        public Guid? CmsDocumentId { get; set; }

        /// <summary>
        /// TMS Template ID (required if SourceType is TmsTemplate)
        /// </summary>
        public Guid? TmsTemplateId { get; set; }

        /// <summary>
        /// Export format for TMS-generated attachments
        /// </summary>
        public TmsExportFormat? TmsExportFormat { get; set; }

        /// <summary>
        /// File path for custom uploaded files (set by server after upload)
        /// </summary>
        public string? CustomFilePath { get; set; }

        /// <summary>
        /// Display name for the attachment
        /// </summary>
        [StringLength(255)]
        public string? CustomFileName { get; set; }

        /// <summary>
        /// Order in which attachments should appear
        /// </summary>
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// Response containing email template attachment details
    /// </summary>
    public class EmailTemplateAttachmentResponse
    {
        public Guid Id { get; set; }
        public Guid EmailTemplateId { get; set; }
        public AttachmentSourceType SourceType { get; set; }
        public Guid? CmsDocumentId { get; set; }
        public Guid? TmsTemplateId { get; set; }
        public int? TmsExportFormat { get; set; }
        public string? CustomFilePath { get; set; }
        public string? CustomFileName { get; set; }
        public long? FileSize { get; set; }
        public string? MimeType { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
