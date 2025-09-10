using System.ComponentModel.DataAnnotations;

namespace CMS.WebApi.Models
{
    public class Document
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Author { get; set; } = string.Empty;
        
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;
        
        public long Size { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;
    }
}
