using System.ComponentModel.DataAnnotations;

namespace CMS.WebApi.Models
{
    public class RegisterDocumentRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string Author { get; set; } = string.Empty;
        
        [Required]
        public string Type { get; set; } = string.Empty;
        
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
        public string Author { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; }
        public string Type { get; set; } = string.Empty;
        public long Size { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
    }
}
