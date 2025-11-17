using System.ComponentModel.DataAnnotations;

namespace CMS.WebApi.Models
{
    /// <summary>
    /// Request to create a new email template
    /// </summary>
    public class CreateEmailTemplateRequest
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string HtmlContent { get; set; } = string.Empty;

        public string? PlainTextContent { get; set; }

        public Guid? TemplateId { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }
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
    }

    /// <summary>
    /// Response containing email template details
    /// </summary>
    public class EmailTemplateResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string? PlainTextContent { get; set; }
        public Guid? TemplateId { get; set; }
        public bool IsActive { get; set; }
        public string? Category { get; set; }
        public int SentCount { get; set; }
        public int FailureCount { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }

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
}
