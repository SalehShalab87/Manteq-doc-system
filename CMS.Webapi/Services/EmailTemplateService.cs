using Microsoft.EntityFrameworkCore;
using CMS.WebApi.Data;
using CMS.WebApi.Models;

namespace CMS.WebApi.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly CmsDbContext _context;
        private readonly ILogger<EmailTemplateService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmailTemplateService(CmsDbContext context, ILogger<EmailTemplateService> logger , IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<EmailTemplateResponse> CreateEmailTemplateAsync(CreateEmailTemplateRequest request, string createdBy)
        {
            var emailTemplate = new EmailTemplate
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Subject = request.Subject,
                HtmlContent = request.HtmlContent ?? string.Empty,
                PlainTextContent = request.PlainTextContent,
                TemplateId = request.TemplateId,
                Category = request.Category,
                BodySourceType = request.BodySourceType,
                TmsTemplateId = request.TmsTemplateId,
                CustomTemplateFilePath = request.CustomTemplateFilePath,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow
            };

            _context.EmailTemplates.Add(emailTemplate);

            // Add attachments if provided
            if (request.Attachments != null && request.Attachments.Any())
            {
                foreach (var attachmentRequest in request.Attachments)
                {
                    var attachment = new EmailTemplateAttachment
                    {
                        Id = Guid.NewGuid(),
                        EmailTemplateId = emailTemplate.Id,
                        SourceType = attachmentRequest.SourceType,
                        CmsDocumentId = attachmentRequest.CmsDocumentId,
                        TmsTemplateId = attachmentRequest.TmsTemplateId,
                        TmsExportFormat = (int?)attachmentRequest.TmsExportFormat,
                        CustomFilePath = attachmentRequest.CustomFilePath,
                        CustomFileName = attachmentRequest.CustomFileName,
                        DisplayOrder = attachmentRequest.DisplayOrder,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = createdBy
                    };
                    _context.EmailTemplateAttachments.Add(attachment);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Email template created: {TemplateId} - {TemplateName}", emailTemplate.Id, emailTemplate.Name);

            // Return with attachments loaded
            return (await GetEmailTemplateByIdAsync(emailTemplate.Id))!;
        }

        public async Task<EmailTemplateResponse?> GetEmailTemplateByIdAsync(Guid id)
        {
            var emailTemplate = await _context.EmailTemplates
                .Include(e => e.Template)
                .Include(e => e.DefaultAttachments)
                .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

            return emailTemplate != null ? MapToResponse(emailTemplate) : null;
        }

        public async Task<List<EmailTemplateResponse>> GetAllEmailTemplatesAsync(bool? isActive = null, string? category = null)
        {
            var query = _context.EmailTemplates
                .Include(e => e.Template)
                .Where(e => !e.IsDeleted)
                .AsQueryable();

            if (isActive.HasValue)
                query = query.Where(e => e.IsActive == isActive.Value);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(e => e.Category == category);

            var emailTemplates = await query.OrderByDescending(e => e.CreatedDate).ToListAsync();

            return emailTemplates.Select(MapToResponse).ToList();
        }

        public async Task<EmailTemplateResponse?> UpdateEmailTemplateAsync(Guid id, UpdateEmailTemplateRequest request)
        {
            var emailTemplate = await _context.EmailTemplates
                .Include(e => e.DefaultAttachments)
                .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
            
            if (emailTemplate == null)
                return null;

            if (!string.IsNullOrEmpty(request.Name))
                emailTemplate.Name = request.Name;

            if (!string.IsNullOrEmpty(request.Subject))
                emailTemplate.Subject = request.Subject;

            if (!string.IsNullOrEmpty(request.HtmlContent))
                emailTemplate.HtmlContent = request.HtmlContent;

            if (request.PlainTextContent != null)
                emailTemplate.PlainTextContent = request.PlainTextContent;

            if (request.TemplateId.HasValue)
                emailTemplate.TemplateId = request.TemplateId;

            if (request.Category != null)
                emailTemplate.Category = request.Category;

            if (request.BodySourceType.HasValue)
                emailTemplate.BodySourceType = request.BodySourceType.Value;

            if (request.TmsTemplateId.HasValue)
                emailTemplate.TmsTemplateId = request.TmsTemplateId;

            if (request.CustomTemplateFilePath != null)
                emailTemplate.CustomTemplateFilePath = request.CustomTemplateFilePath;

            // Handle attachments if provided
            if (request.Attachments != null)
            {
                // Load existing attachments for this template
                var existingAttachments = await _context.EmailTemplateAttachments
                    .Where(a => a.EmailTemplateId == id)
                    .ToListAsync();

                // Remove all existing attachments
                if (existingAttachments.Any())
                {
                    _context.EmailTemplateAttachments.RemoveRange(existingAttachments);
                    await _context.SaveChangesAsync(); // Save deletion separately
                }

                // Add new attachments
                foreach (var attachmentRequest in request.Attachments)
                {
                    var attachment = new EmailTemplateAttachment
                    {
                        Id = Guid.NewGuid(),
                        EmailTemplateId = id,
                        SourceType = attachmentRequest.SourceType,
                        CmsDocumentId = attachmentRequest.CmsDocumentId,
                        TmsTemplateId = attachmentRequest.TmsTemplateId,
                        TmsExportFormat = (int?)attachmentRequest.TmsExportFormat,
                        CustomFilePath = attachmentRequest.CustomFilePath,
                        CustomFileName = attachmentRequest.CustomFileName,
                        DisplayOrder = attachmentRequest.DisplayOrder,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = GetCurrentUserId(), // TODO: Get from context
                    };
                    _context.EmailTemplateAttachments.Add(attachment);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Email template updated: {TemplateId}", id);

            // Reload the entity with navigation properties to return complete response
            return await GetEmailTemplateByIdAsync(id);
        }

        public async Task<bool> DeleteEmailTemplateAsync(Guid id)
        {
            var emailTemplate = await _context.EmailTemplates.FindAsync(id);
            if (emailTemplate == null || emailTemplate.IsDeleted)
                return false;

            // Soft delete
            emailTemplate.IsDeleted = true;
            emailTemplate.DeletedAt = DateTime.UtcNow;
            emailTemplate.DeletedBy = GetCurrentUserId(); // TODO: Get from HttpContext
            
            await _context.SaveChangesAsync();

            _logger.LogInformation("Email template moved to trash: {TemplateId}", id);

            return true;
        }

        public async Task<bool> ActivateEmailTemplateAsync(Guid id)
        {
            var emailTemplate = await _context.EmailTemplates.FindAsync(id);
            if (emailTemplate == null || emailTemplate.IsDeleted)
                return false;

            emailTemplate.IsActive = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Email template activated: {TemplateId}", id);

            return true;
        }

        public async Task<bool> DeactivateEmailTemplateAsync(Guid id)
        {
            var emailTemplate = await _context.EmailTemplates.FindAsync(id);
            if (emailTemplate == null || emailTemplate.IsDeleted)
                return false;

            emailTemplate.IsActive = false;
            emailTemplate.IsDeleted = true;
            emailTemplate.DeletedAt = DateTime.UtcNow;
            emailTemplate.DeletedBy = GetCurrentUserId(); // TODO: Get from HttpContext
            await _context.SaveChangesAsync();

            _logger.LogInformation("Email template moved to trash: {TemplateId}", id);

            return true;
        }

        public async Task<bool> IncrementSentCountAsync(Guid id)
        {
            var emailTemplate = await _context.EmailTemplates.FindAsync(id);
            if (emailTemplate == null || emailTemplate.IsDeleted)
                return false;

            emailTemplate.SentCount++;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Email template sent count incremented: {TemplateId}, Count: {Count}", 
                id, emailTemplate.SentCount);

            return true;
        }

        public async Task<bool> IncrementFailureCountAsync(Guid id)
        {
            var emailTemplate = await _context.EmailTemplates.FindAsync(id);
            if (emailTemplate == null || emailTemplate.IsDeleted)
                return false;

            emailTemplate.FailureCount++;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Email template failure count incremented: {TemplateId}, Count: {Count}", 
                id, emailTemplate.FailureCount);

            return true;
        }

        public async Task<EmailTemplateAnalyticsResponse?> GetEmailTemplateAnalyticsAsync(Guid id)
        {
            var emailTemplate = await _context.EmailTemplates.FindAsync(id);
            if (emailTemplate == null || emailTemplate.IsDeleted)
                return null;

            var totalAttempts = emailTemplate.SentCount + emailTemplate.FailureCount;
            var successRate = totalAttempts > 0 
                ? (double)emailTemplate.SentCount / totalAttempts * 100 
                : 0;

            return new EmailTemplateAnalyticsResponse
            {
                TemplateId = emailTemplate.Id,
                TemplateName = emailTemplate.Name,
                SentCount = emailTemplate.SentCount,
                FailureCount = emailTemplate.FailureCount,
                TotalAttempts = totalAttempts,
                SuccessRate = Math.Round(successRate, 2)
            };
        }

        // Additional methods for direct entity access
        public async Task<EmailTemplate?> GetTemplateByIdAsync(Guid id)
        {
            return await _context.EmailTemplates
                .Include(e => e.DefaultAttachments)
                .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        }

        public async Task<List<EmailTemplate>> GetAllTemplatesAsync()
        {
            return await _context.EmailTemplates
                .Include(e => e.DefaultAttachments)
                .Where(e => !e.IsDeleted)
                .OrderByDescending(e => e.CreatedDate)
                .ToListAsync();
        }

        public async Task<EmailTemplate> CreateTemplateAsync(CreateEmailTemplateRequest request)
        {
            var emailTemplate = new EmailTemplate
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Subject = request.Subject,
                HtmlContent = request.HtmlContent,
                PlainTextContent = request.PlainTextContent,
                TemplateId = request.TemplateId,
                Category = request.Category,
                BodySourceType = request.BodySourceType,
                TmsTemplateId = request.TmsTemplateId,
                CustomTemplateFilePath = request.CustomTemplateFilePath,
                IsActive = true,
                CreatedBy = GetCurrentUserId(),
                CreatedDate = DateTime.UtcNow
            };

            _context.EmailTemplates.Add(emailTemplate);

            // Add attachments if provided
            if (request.Attachments != null && request.Attachments.Any())
            {
                foreach (var attachmentRequest in request.Attachments)
                {
                    var attachment = new EmailTemplateAttachment
                    {
                        Id = Guid.NewGuid(),
                        EmailTemplateId = emailTemplate.Id,
                        SourceType = attachmentRequest.SourceType,
                        CmsDocumentId = attachmentRequest.CmsDocumentId,
                        TmsTemplateId = attachmentRequest.TmsTemplateId,
                        TmsExportFormat = (int?)attachmentRequest.TmsExportFormat,
                        CustomFilePath = attachmentRequest.CustomFilePath,
                        CustomFileName = attachmentRequest.CustomFileName,
                        DisplayOrder = attachmentRequest.DisplayOrder
                    };

                    _context.EmailTemplateAttachments.Add(attachment);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Email template created: {TemplateId} - {TemplateName}", emailTemplate.Id, emailTemplate.Name);

            // Reload with attachments
            return (await GetTemplateByIdAsync(emailTemplate.Id))!;
        }

        public async Task<EmailTemplate?> UpdateTemplateAsync(Guid id, CreateEmailTemplateRequest request)
        {
            var emailTemplate = await _context.EmailTemplates
                .Include(e => e.DefaultAttachments)
                .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

            if (emailTemplate == null)
                return null;

            emailTemplate.Name = request.Name;
            emailTemplate.Subject = request.Subject;
            emailTemplate.HtmlContent = request.HtmlContent;
            emailTemplate.PlainTextContent = request.PlainTextContent;
            emailTemplate.TemplateId = request.TemplateId;
            emailTemplate.Category = request.Category;
            emailTemplate.BodySourceType = request.BodySourceType;
            emailTemplate.TmsTemplateId = request.TmsTemplateId;
            emailTemplate.CustomTemplateFilePath = request.CustomTemplateFilePath;

            // Update attachments - remove old ones and add new ones
            _context.EmailTemplateAttachments.RemoveRange(emailTemplate.DefaultAttachments);

            if (request.Attachments != null && request.Attachments.Any())
            {
                foreach (var attachmentRequest in request.Attachments)
                {
                    var attachment = new EmailTemplateAttachment
                    {
                        Id = Guid.NewGuid(),
                        EmailTemplateId = emailTemplate.Id,
                        SourceType = attachmentRequest.SourceType,
                        CmsDocumentId = attachmentRequest.CmsDocumentId,
                        TmsTemplateId = attachmentRequest.TmsTemplateId,
                        TmsExportFormat = (int?)attachmentRequest.TmsExportFormat,
                        CustomFilePath = attachmentRequest.CustomFilePath,
                        CustomFileName = attachmentRequest.CustomFileName,
                        DisplayOrder = attachmentRequest.DisplayOrder
                    };

                    _context.EmailTemplateAttachments.Add(attachment);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Email template updated: {TemplateId} - {TemplateName}", emailTemplate.Id, emailTemplate.Name);

            return emailTemplate;
        }

        public async Task<bool> DeleteTemplateAsync(Guid id)
        {
            var emailTemplate = await _context.EmailTemplates.FindAsync(id);
            if (emailTemplate == null || emailTemplate.IsDeleted)
                return false;

            emailTemplate.IsDeleted = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Email template deleted: {TemplateId}", id);
            return true;
        }

        public async Task<List<EmailTemplateAttachment>> GetTemplateAttachmentsAsync(Guid templateId)
        {
            return await _context.EmailTemplateAttachments
                .Where(a => a.EmailTemplateId == templateId)
                .OrderBy(a => a.DisplayOrder)
                .ToListAsync();
        }

        private string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.Request.Headers["X-SME-UserId"].FirstOrDefault()
                   ?? "SYSTEM";
        }

        private static EmailTemplateResponse MapToResponse(EmailTemplate emailTemplate)
        {
            return new EmailTemplateResponse
            {
                Id = emailTemplate.Id,
                Name = emailTemplate.Name,
                Subject = emailTemplate.Subject,
                HtmlContent = emailTemplate.HtmlContent,
                PlainTextContent = emailTemplate.PlainTextContent,
                TemplateId = emailTemplate.TemplateId,
                BodySourceType = emailTemplate.BodySourceType,
                TmsTemplateId = emailTemplate.TmsTemplateId,
                CustomTemplateFilePath = emailTemplate.CustomTemplateFilePath,
                IsActive = emailTemplate.IsActive,
                Category = emailTemplate.Category,
                SentCount = emailTemplate.SentCount,
                FailureCount = emailTemplate.FailureCount,
                CreatedBy = emailTemplate.CreatedBy,
                CreatedDate = emailTemplate.CreatedDate,
                Attachments = emailTemplate.DefaultAttachments?.Select(a => new EmailTemplateAttachmentResponse
                {
                    Id = a.Id,
                    EmailTemplateId = a.EmailTemplateId,
                    SourceType = a.SourceType,
                    CmsDocumentId = a.CmsDocumentId,
                    TmsTemplateId = a.TmsTemplateId,
                    TmsExportFormat = a.TmsExportFormat,
                    CustomFilePath = a.CustomFilePath,
                    CustomFileName = a.CustomFileName,
                    FileSize = a.FileSize,
                    MimeType = a.MimeType,
                    DisplayOrder = a.DisplayOrder,
                    CreatedDate = a.CreatedDate,
                    CreatedBy = a.CreatedBy
                }).OrderBy(a => a.DisplayOrder).ToList()
            };
        }
    }
}
