using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMS.WebApi.Data;
using CMS.WebApi.Models;

namespace CMS.WebApi.Controllers
{
    public class TrashResponse
    {
        public List<TrashItemDto> Documents { get; set; } = new();
        public List<TrashItemDto> Templates { get; set; } = new();
        public List<TrashItemDto> EmailTemplates { get; set; } = new();
    }

    public class TrashItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Document", "Template", "EmailTemplate"
        public DateTime DeletedAt { get; set; }
        public string DeletedBy { get; set; } = string.Empty;
        public string? OriginalType { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class TrashController : ControllerBase
{
    private readonly CmsDbContext _context;
    private readonly ILogger<TrashController> _logger;

    public TrashController(CmsDbContext context, ILogger<TrashController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all deleted items (documents, templates, email templates)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<TrashResponse>> GetDeletedItems()
    {
        try
        {
            var deletedDocuments = await _context.Documents
                .Where(d => d.IsDeleted)
                .OrderByDescending(d => d.DeletedAt)
                .ToListAsync();

            var deletedTemplates = await _context.Templates
                .Where(t => t.IsDeleted)
                .OrderByDescending(t => t.DeletedAt)
                .ToListAsync();

            var deletedEmailTemplates = await _context.EmailTemplates
                .Where(e => e.IsDeleted)
                .OrderByDescending(e => e.DeletedAt)
                .ToListAsync();

            var response = new TrashResponse
            {
                Documents = deletedDocuments.Select(d => new TrashItemDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Type = "Document",
                    DeletedAt = d.DeletedAt ?? DateTime.UtcNow,
                    DeletedBy = d.DeletedBy ?? "Unknown",
                    OriginalType = d.Type
                }).ToList(),

                Templates = deletedTemplates.Select(t => new TrashItemDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Type = "Template",
                    DeletedAt = t.DeletedAt ?? DateTime.UtcNow,
                    DeletedBy = t.DeletedBy ?? "Unknown",
                    OriginalType = t.Category
                }).ToList(),

                EmailTemplates = deletedEmailTemplates.Select(e => new TrashItemDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Type = "EmailTemplate",
                    DeletedAt = e.DeletedAt ?? DateTime.UtcNow,
                    DeletedBy = e.DeletedBy ?? "Unknown",
                    OriginalType = e.Category
                }).ToList()
            };

            _logger.LogInformation("Retrieved {DocumentCount} documents, {TemplateCount} templates, {EmailCount} email templates from trash",
                response.Documents.Count, response.Templates.Count, response.EmailTemplates.Count);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deleted items");
            return StatusCode(500, "Error retrieving deleted items");
        }
    }

    /// <summary>
    /// Restore a deleted document
    /// </summary>
    [HttpPost("documents/{id}/restore")]
    public async Task<IActionResult> RestoreDocument(Guid id)
    {
        try
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null || !document.IsDeleted)
            {
                return NotFound("Document not found in trash");
            }

            document.IsDeleted = false;
            document.DeletedAt = null;
            document.DeletedBy = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Document {DocumentId} restored from trash", id);
            return Ok(new { message = "Document restored successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring document {DocumentId}", id);
            return StatusCode(500, "Error restoring document");
        }
    }

    /// <summary>
    /// Restore a deleted template
    /// </summary>
    [HttpPost("templates/{id}/restore")]
    public async Task<IActionResult> RestoreTemplate(Guid id)
    {
        try
        {
            var template = await _context.Templates.FindAsync(id);
            if (template == null || !template.IsDeleted)
            {
                return NotFound("Template not found in trash");
            }

            template.IsDeleted = false;
            template.DeletedAt = null;
            template.DeletedBy = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Template {TemplateId} restored from trash", id);
            return Ok(new { message = "Template restored successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring template {TemplateId}", id);
            return StatusCode(500, "Error restoring template");
        }
    }

    /// <summary>
    /// Restore a deleted email template
    /// </summary>
    [HttpPost("email-templates/{id}/restore")]
    public async Task<IActionResult> RestoreEmailTemplate(Guid id)
    {
        try
        {
            var emailTemplate = await _context.EmailTemplates.FindAsync(id);
            if (emailTemplate == null || !emailTemplate.IsDeleted)
            {
                return NotFound("Email template not found in trash");
            }

            emailTemplate.IsDeleted = false;
            emailTemplate.DeletedAt = null;
            emailTemplate.DeletedBy = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Email template {EmailTemplateId} restored from trash", id);
            return Ok(new { message = "Email template restored successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring email template {EmailTemplateId}", id);
            return StatusCode(500, "Error restoring email template");
        }
    }

    /// <summary>
    /// Permanently delete a document
    /// </summary>
    [HttpDelete("documents/{id}/permanent")]
    public async Task<IActionResult> PermanentlyDeleteDocument(Guid id)
    {
        try
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null || !document.IsDeleted)
            {
                return NotFound("Document not found in trash");
            }

            // Delete physical file
            if (System.IO.File.Exists(document.FilePath))
            {
                System.IO.File.Delete(document.FilePath);
            }

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Document {DocumentId} permanently deleted", id);
            return Ok(new { message = "Document permanently deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error permanently deleting document {DocumentId}", id);
            return StatusCode(500, "Error permanently deleting document");
        }
    }

    /// <summary>
    /// Permanently delete a template
    /// </summary>
    [HttpDelete("templates/{id}/permanent")]
    public async Task<IActionResult> PermanentlyDeleteTemplate(Guid id)
    {
        try
        {
            var template = await _context.Templates.FindAsync(id);
            if (template == null || !template.IsDeleted)
            {
                return NotFound("Template not found in trash");
            }

            _context.Templates.Remove(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Template {TemplateId} permanently deleted", id);
            return Ok(new { message = "Template permanently deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error permanently deleting template {TemplateId}", id);
            return StatusCode(500, "Error permanently deleting template");
        }
    }

    /// <summary>
    /// Permanently delete an email template
    /// </summary>
    [HttpDelete("email-templates/{id}/permanent")]
    public async Task<IActionResult> PermanentlyDeleteEmailTemplate(Guid id)
    {
        try
        {
            var emailTemplate = await _context.EmailTemplates.FindAsync(id);
            if (emailTemplate == null || !emailTemplate.IsDeleted)
            {
                return NotFound("Email template not found in trash");
            }

            _context.EmailTemplates.Remove(emailTemplate);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Email template {EmailTemplateId} permanently deleted", id);
            return Ok(new { message = "Email template permanently deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error permanently deleting email template {EmailTemplateId}", id);
            return StatusCode(500, "Error permanently deleting email template");
        }
    }

    /// <summary>
    /// Empty entire trash
    /// </summary>
    [HttpDelete("empty")]
    public async Task<IActionResult> EmptyTrash()
    {
        try
        {
            var deletedDocuments = await _context.Documents.Where(d => d.IsDeleted).ToListAsync();
            var deletedTemplates = await _context.Templates.Where(t => t.IsDeleted).ToListAsync();
            var deletedEmailTemplates = await _context.EmailTemplates.Where(e => e.IsDeleted).ToListAsync();

            // Delete physical files for documents
            foreach (var doc in deletedDocuments)
            {
                if (System.IO.File.Exists(doc.FilePath))
                {
                    System.IO.File.Delete(doc.FilePath);
                }
            }

            _context.Documents.RemoveRange(deletedDocuments);
            _context.Templates.RemoveRange(deletedTemplates);
            _context.EmailTemplates.RemoveRange(deletedEmailTemplates);

            await _context.SaveChangesAsync();

            var totalDeleted = deletedDocuments.Count + deletedTemplates.Count + deletedEmailTemplates.Count;
            _logger.LogInformation("Emptied trash: {DocumentCount} documents, {TemplateCount} templates, {EmailCount} email templates",
                deletedDocuments.Count, deletedTemplates.Count, deletedEmailTemplates.Count);

            return Ok(new { 
                message = "Trash emptied successfully", 
                totalDeleted = totalDeleted,
                documents = deletedDocuments.Count,
                templates = deletedTemplates.Count,
                emailTemplates = deletedEmailTemplates.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error emptying trash");
            return StatusCode(500, "Error emptying trash");
        }
    }
    }
}
