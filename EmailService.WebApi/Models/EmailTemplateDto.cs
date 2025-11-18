namespace EmailService.WebApi.Models
{
    /// <summary>
    /// Email template response from CMS (clean DTO without CMS references)
    /// </summary>
    public class EmailTemplateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public EmailBodySourceType BodySourceType { get; set; }
        public string? PlainTextContent { get; set; }
        public Guid? TmsTemplateId { get; set; }
        public string? CustomTemplateFilePath { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public int SentCount { get; set; }
        public int FailureCount { get; set; }
        public List<EmailTemplateAttachmentDto>? Attachments { get; set; }
    }

    /// <summary>
    /// Email template attachment DTO
    /// </summary>
    public class EmailTemplateAttachmentDto
    {
        public Guid Id { get; set; }
        public Guid EmailTemplateId { get; set; }
        public AttachmentSourceType SourceType { get; set; }
        public Guid? CmsDocumentId { get; set; }
        public Guid? TmsTemplateId { get; set; }
        public TmsExportFormat? TmsExportFormat { get; set; }
        public string? CustomFilePath { get; set; }
        public string? CustomFileName { get; set; }
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// Email body source types (where the email HTML comes from)
    /// </summary>
    public enum EmailBodySourceType
    {
        PlainText = 0,      // Simple plain text body
        TmsTemplate = 1,    // Generated from TMS template
        CustomTemplate = 2  // Custom HTML file stored in CMS
    }

    /// <summary>
    /// Attachment source types (where attachments come from)
    /// </summary>
    public enum AttachmentSourceType
    {
        CmsDocument = 0,    // Existing document in CMS
        TmsTemplate = 1,    // Generated from TMS template
        CustomFile = 2      // Custom file stored in CMS
    }
}
