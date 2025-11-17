using System.ComponentModel.DataAnnotations;

namespace CMS.WebApi.Models
{
    public class RegisterDocumentRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Type { get; set; }  // Optional: Invoice, Contract, Report, Letter
        
        [Required]
        public IFormFile Content { get; set; } = null!;
    }
    
    public class RegisterDocumentResponse
    {
        public Guid Id { get; set; }
        public string Message { get; set; } = string.Empty;
    }
    
    public class RetrieveDocumentResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Type { get; set; }
        public long Size { get; set; }
        public string Extension { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
    }
}
