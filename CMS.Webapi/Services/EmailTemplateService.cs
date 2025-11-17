using Microsoft.EntityFrameworkCore;
using CMS.WebApi.Data;
using CMS.WebApi.Models;

namespace CMS.WebApi.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly CmsDbContext _context;
        private readonly ILogger<EmailTemplateService> _logger;

        public EmailTemplateService(CmsDbContext context, ILogger<EmailTemplateService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<EmailTemplateResponse> CreateEmailTemplateAsync(CreateEmailTemplateRequest request, string createdBy)
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
                IsActive = true,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow
            };

            _context.EmailTemplates.Add(emailTemplate);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Email template created: {TemplateId} - {TemplateName}", emailTemplate.Id, emailTemplate.Name);

            return MapToResponse(emailTemplate);
        }

        public async Task<EmailTemplateResponse?> GetEmailTemplateByIdAsync(Guid id)
        {
            var emailTemplate = await _context.EmailTemplates
                .Include(e => e.Template)
                .FirstOrDefaultAsync(e => e.Id == id);

            return emailTemplate != null ? MapToResponse(emailTemplate) : null;
        }

        public async Task<List<EmailTemplateResponse>> GetAllEmailTemplatesAsync(bool? isActive = null, string? category = null)
        {
            var query = _context.EmailTemplates.Include(e => e.Template).AsQueryable();

            if (isActive.HasValue)
                query = query.Where(e => e.IsActive == isActive.Value);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(e => e.Category == category);

            var emailTemplates = await query.OrderByDescending(e => e.CreatedDate).ToListAsync();

            return emailTemplates.Select(MapToResponse).ToList();
        }

        public async Task<EmailTemplateResponse?> UpdateEmailTemplateAsync(Guid id, UpdateEmailTemplateRequest request)
        {
            var emailTemplate = await _context.EmailTemplates.FindAsync(id);
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

            await _context.SaveChangesAsync();

            _logger.LogInformation("Email template updated: {TemplateId}", id);

            return MapToResponse(emailTemplate);
        }

        public async Task<bool> DeleteEmailTemplateAsync(Guid id)
        {
            var emailTemplate = await _context.EmailTemplates.FindAsync(id);
            if (emailTemplate == null)
                return false;

            _context.EmailTemplates.Remove(emailTemplate);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Email template deleted: {TemplateId}", id);

            return true;
        }

        public async Task<bool> ActivateEmailTemplateAsync(Guid id)
        {
            var emailTemplate = await _context.EmailTemplates.FindAsync(id);
            if (emailTemplate == null)
                return false;

            emailTemplate.IsActive = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Email template activated: {TemplateId}", id);

            return true;
        }

        public async Task<bool> DeactivateEmailTemplateAsync(Guid id)
        {
            var emailTemplate = await _context.EmailTemplates.FindAsync(id);
            if (emailTemplate == null)
                return false;

            emailTemplate.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Email template deactivated: {TemplateId}", id);

            return true;
        }

        public async Task<bool> IncrementSentCountAsync(Guid id)
        {
            var emailTemplate = await _context.EmailTemplates.FindAsync(id);
            if (emailTemplate == null)
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
            if (emailTemplate == null)
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
            if (emailTemplate == null)
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
                IsActive = emailTemplate.IsActive,
                Category = emailTemplate.Category,
                SentCount = emailTemplate.SentCount,
                FailureCount = emailTemplate.FailureCount,
                CreatedBy = emailTemplate.CreatedBy,
                CreatedDate = emailTemplate.CreatedDate
            };
        }
    }
}
