using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TMS.WebApi.Models
{
    public class RegisterTemplateRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public string Category { get; set; } = string.Empty;
        
        [Required]
        public IFormFile TemplateFile { get; set; } = null!;
        
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class RegisterTemplateResponse
    {
        public Guid TemplateId { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> ExtractedPlaceholders { get; set; } = new();
    }

    public class RetrieveTemplateResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public Guid CmsDocumentId { get; set; }
        public List<string> Placeholders { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string TemplateDownloadUrl { get; set; } = string.Empty;
    }

    public class TemplatePropertiesResponse
    {
        public Guid TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public List<TemplateProperty> Properties { get; set; } = new();
    }

    public class TemplateProperty
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Text"; // Default to text, can be extended later
        public bool IsRequired { get; set; } = true;
        public string Description { get; set; } = string.Empty;
        public string CurrentValue { get; set; } = string.Empty; // For showing current/default values
    }

    public class DocumentGenerationRequest
    {
        [Required]
        public Guid TemplateId { get; set; }
        
        [Required]
        public Dictionary<string, string> PropertyValues { get; set; } = new();
        
        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ExportFormat ExportFormat { get; set; } = ExportFormat.Original;
        
        public string GeneratedBy { get; set; } = string.Empty;
    }

    public class DocumentGenerationResponse
    {
        public Guid GenerationId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public ExportFormat ExportFormat { get; set; }
        public int ProcessedPlaceholders { get; set; }
    }

    public enum ExportFormat
    {
        Original,    // Keep original format (Word/Excel/PowerPoint)
        Word,        // Convert to Word format (.docx)
        Html,        // HTML with external images
        EmailHtml,   // HTML with embedded base64 images (email-friendly)
        Pdf          // PDF format
    }

    public enum DocumentType
    {
        Word,        // .docx
        Excel,       // .xlsx  
        PowerPoint   // .pptx
    }

    // Internal model for temporary file management
    public class GeneratedDocument
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public ExportFormat ExportFormat { get; set; }
        public Guid SourceTemplateId { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
    }

    // Document Embedding Models
    public class DocumentEmbeddingRequest
    {
        [Required]
        public Guid MainTemplateId { get; set; }
        
        [Required]
        public Dictionary<string, string> MainTemplateValues { get; set; } = new();
        
        [Required]
        public List<EmbedInfo> Embeddings { get; set; } = new();
        
        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ExportFormat ExportFormat { get; set; } = ExportFormat.Original;
        
        public string GeneratedBy { get; set; } = string.Empty;
    }

    public class EmbedInfo
    {
        [Required]
        public Guid EmbedTemplateId { get; set; }
        
        [Required]
        public Dictionary<string, string> EmbedTemplateValues { get; set; } = new();
        
        [Required]
        public string EmbedPlaceholder { get; set; } = string.Empty;
    }

    public class DocumentEmbeddingResponse
    {
        public Guid GenerationId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public ExportFormat ExportFormat { get; set; }
        public int ProcessedEmbeddings { get; set; }
        public int ProcessedMainPlaceholders { get; set; }
        public List<string> EmbeddingResults { get; set; } = new();
    }

    /// <summary>
    /// Request model for testing document generation with Excel file upload
    /// </summary>
    public class TestGenerateRequest
    {
        [Required]
        public IFormFile ExcelFile { get; set; } = null!;
        
        public ExportFormat ExportFormat { get; set; } = ExportFormat.Word;
    }

    /// <summary>
    /// Request model for testing template without saving
    /// Requires both template file and filled Excel file
    /// </summary>
    public class TestTemplateRequest
    {
        [Required]
        public IFormFile TemplateFile { get; set; } = null!;
        
        [Required]
        public IFormFile ExcelFile { get; set; } = null!;
        
        public ExportFormat ExportFormat { get; set; } = ExportFormat.Word;
    }
}

