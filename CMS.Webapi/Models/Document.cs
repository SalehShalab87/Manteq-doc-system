using System.ComponentModel.DataAnnotations;

namespace CMS.WebApi.Models
{
    public class Document
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string? Type { get; set; }  // Invoice, Contract, Report, Letter
        
        [Required]
        public long Size { get; set; }
        
        [Required]
        [MaxLength(10)]
        public string Extension { get; set; } = string.Empty;  // docx, pdf, xlsx, pptx, txt
        
        [Required]
        [MaxLength(100)]
        public string MimeType { get; set; } = string.Empty;
        
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;
        
        [Required]
        public bool IsActive { get; set; } = true;
        
        [Required]
        [MaxLength(100)]
        public string CreatedBy { get; set; } = string.Empty;  // From X-SME-UserId header
    }
}
