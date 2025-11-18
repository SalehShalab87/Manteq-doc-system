using System.ComponentModel.DataAnnotations;

namespace CMS.WebApi.Models;

public enum TemplateType
{
    Document = 0,      // General document
    TOB = 1,           // Technical Order Bulletin
    Quotation = 2      // Quotation Report
}

public enum ExportFormat
{
    Original = 0,      // Keep original format
    Word = 1,          // Convert to .docx
    PDF = 4            // Convert to PDF
}

public class Template
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    public TemplateType TemplateType { get; set; } = TemplateType.Document;

    [Required]
    public ExportFormat DefaultExportFormat { get; set; } = ExportFormat.Word;

    [Required]
    public Guid CmsDocumentId { get; set; }

    public List<string> Placeholders { get; set; } = new List<string>();

    [Required]
    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
    
    [StringLength(100)]
    public string? DeletedBy { get; set; }

    [Required]
    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? UpdatedBy { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int SuccessCount { get; set; } = 0;

    public int FailureCount { get; set; } = 0;
}
