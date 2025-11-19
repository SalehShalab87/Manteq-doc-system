using CMS.WebApi.Data;
using CMS.WebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.WebApi.Services
{
    public class CmsTemplateService : ICmsTemplateService
    {
        private readonly CmsDbContext _context;
        private readonly ILogger<CmsTemplateService> _logger;

        public CmsTemplateService(CmsDbContext context, ILogger<CmsTemplateService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Template> CreateTemplateAsync(Template template)
        {
            try
            {
                template.Id = Guid.NewGuid();
                template.CreatedAt = DateTime.UtcNow;
                template.UpdatedAt = DateTime.UtcNow;

                _context.Templates.Add(template);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Template created successfully with ID: {TemplateId}", template.Id);
                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating template: {TemplateName}", template.Name);
                throw;
            }
        }

        public async Task<Template?> GetTemplateByIdAsync(Guid id)
        {
            try
            {
                return await _context.Templates
                    .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving template by ID: {TemplateId}", id);
                throw;
            }
        }

        public async Task<List<Template>> GetAllTemplatesAsync()
        {
            try
            {
                return await _context.Templates
                    .Where(t => !t.IsDeleted)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all templates");
                throw;
            }
        }

        public async Task<List<string>> GetTemplatePlaceholdersAsync(string name, bool isActive = true)
        {
            try
            {
                var template = await _context.Templates
                    .FirstOrDefaultAsync(t => t.Name == name && t.IsActive == isActive && !t.IsDeleted);

                if (template == null)
                {
                    throw new Exception($"Template with name '{name}' not found or inactive.");
                }

                return template.Placeholders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving template placeholders for: {TemplateName}", name);
                throw;
            }
        }

        public async Task<List<Template>> GetTemplatesByNameAsync(string name)
        {
            try
            {
                return await _context.Templates
                    .Where(t => t.Name.Contains(name, StringComparison.OrdinalIgnoreCase) && !t.IsDeleted)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving templates by name: {Name}", name);
                throw;
            }
        }

        public async Task<List<Template>> GetActiveTemplatesAsync()
        {
            try
            {
                return await _context.Templates
                    .Where(t => t.IsActive && !t.IsDeleted)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active templates");
                throw;
            }
        }

        public async Task<List<Template>> GetTemplatesByCategoryAsync(string category)
        {
            try
            {
                return await _context.Templates
                    .Where(t => t.Category == category && t.IsActive && !t.IsDeleted)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving templates by category: {Category}", category);
                throw;
            }
        }

        public async Task<Template?> UpdateTemplateAsync(Guid id, Template template)
        {
            try
            {
                var existingTemplate = await _context.Templates.FindAsync(id);
                if (existingTemplate == null)
                {
                    return null;
                }

                // Update properties
                existingTemplate.Name = template.Name;
                existingTemplate.Description = template.Description;
                existingTemplate.Category = template.Category;
                existingTemplate.Placeholders = template.Placeholders;
                existingTemplate.UpdatedAt = DateTime.UtcNow;
                existingTemplate.UpdatedBy = template.UpdatedBy;
                existingTemplate.IsActive = template.IsActive;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Template updated successfully: {TemplateId}", id);
                return existingTemplate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating template: {TemplateId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteTemplateAsync(Guid id)
        {
            try
            {
                var template = await _context.Templates.FindAsync(id);
                if (template == null || template.IsDeleted)
                {
                    return false;
                }

                // Soft delete
                template.IsDeleted = true;
                template.DeletedAt = DateTime.UtcNow;
                template.DeletedBy = "SYSTEM"; // TODO: Get from HttpContext
                template.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("Template moved to trash: {TemplateId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template: {TemplateId}", id);
                throw;
            }
        }

        public async Task<bool> DeactivateTemplateAsync(Guid id)
        {
            try
            {
                var template = await _context.Templates.FindAsync(id);
                if (template == null)
                {
                    return false;
                }

                template.IsActive = false;
                template.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("Template deactivated successfully: {TemplateId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating template: {TemplateId}", id);
                throw;
            }
        }

        public async Task<bool> ActivateTemplateAsync(Guid id)
        {
            try
            {
                var template = await _context.Templates.FindAsync(id);
                if (template == null)
                {
                    return false;
                }

                template.IsActive = true;
                template.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("Template activated successfully: {TemplateId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating template: {TemplateId}", id);
                throw;
            }
        }

        public async Task<bool> IncrementSuccessCountAsync(Guid id)
        {
            try
            {
                var template = await _context.Templates.FindAsync(id);
                if (template == null)
                {
                    return false;
                }

                template.SuccessCount++;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Template success count incremented: {TemplateId}, Count: {Count}", 
                    id, template.SuccessCount);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing success count for template: {TemplateId}", id);
                throw;
            }
        }

        public async Task<bool> IncrementFailureCountAsync(Guid id)
        {
            try
            {
                var template = await _context.Templates.FindAsync(id);
                if (template == null)
                {
                    return false;
                }

                template.FailureCount++;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Template failure count incremented: {TemplateId}, Count: {Count}", 
                    id, template.FailureCount);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing failure count for template: {TemplateId}", id);
                throw;
            }
        }
    }
}
