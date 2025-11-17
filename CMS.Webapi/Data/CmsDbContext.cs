using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using CMS.WebApi.Models;
using System.Text.Json;

namespace CMS.WebApi.Data
{
    public class CmsDbContext : DbContext
    {
        public CmsDbContext(DbContextOptions<CmsDbContext> options) : base(options)
        {
        }

        public DbSet<Document> Documents { get; set; }
        public DbSet<Template> Templates { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Document entity
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Extension).IsRequired().HasMaxLength(10);
                entity.Property(e => e.MimeType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Type).HasMaxLength(50);
                
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.CreationDate);
                entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_Documents_IsActive");
                entity.HasIndex(e => e.CreatedBy).HasDatabaseName("IX_Documents_CreatedBy");
                entity.HasIndex(e => e.Extension).HasDatabaseName("IX_Documents_Extension");
            });

            // Configure Template entity
            modelBuilder.Entity<Template>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CmsDocumentId).IsRequired();
                entity.Property(e => e.CreatedBy).HasMaxLength(100);
                entity.Property(e => e.UpdatedBy).HasMaxLength(100);
                
                // Configure enums as integers
                entity.Property(e => e.TemplateType).HasConversion<int>();
                entity.Property(e => e.DefaultExportFormat).HasConversion<int>();
                
                // Configure Placeholders as JSON column
                entity.Property(e => e.Placeholders)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                        v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default) ?? new List<string>()
                    )
                    .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                        (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                        c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v != null ? v.GetHashCode() : 0)),
                        c => c == null ? new List<string>() : c.ToList()
                    ));

                // Add indexes for better query performance
                entity.HasIndex(e => e.Name).HasDatabaseName("IX_Templates_Name");
                entity.HasIndex(e => e.Category).HasDatabaseName("IX_Templates_Category");
                entity.HasIndex(e => e.CmsDocumentId).HasDatabaseName("IX_Templates_CmsDocumentId");
                entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_Templates_IsActive");
                entity.HasIndex(e => e.TemplateType).HasDatabaseName("IX_Templates_TemplateType");
            });

            // Configure EmailTemplate entity
            modelBuilder.Entity<EmailTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
                entity.Property(e => e.HtmlContent).IsRequired();
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                
                // Configure foreign key relationship
                entity.HasOne(e => e.Template)
                    .WithMany()
                    .HasForeignKey(e => e.TemplateId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // Add indexes
                entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_EmailTemplates_IsActive");
                entity.HasIndex(e => e.Category).HasDatabaseName("IX_EmailTemplates_Category");
                entity.HasIndex(e => e.CreatedBy).HasDatabaseName("IX_EmailTemplates_CreatedBy");
                entity.HasIndex(e => e.TemplateId).HasDatabaseName("IX_EmailTemplates_TemplateId");
            });
        }
    }
}
