using CMS.WebApi.Models;

namespace CMS.WebApi.Services
{
    public interface IEmailTemplateService
    {
        Task<EmailTemplateResponse> CreateEmailTemplateAsync(CreateEmailTemplateRequest request, string createdBy);
        Task<EmailTemplateResponse?> GetEmailTemplateByIdAsync(Guid id);
        Task<List<EmailTemplateResponse>> GetAllEmailTemplatesAsync(bool? isActive = null, string? category = null);
        Task<EmailTemplateResponse?> UpdateEmailTemplateAsync(Guid id, UpdateEmailTemplateRequest request);
        Task<bool> DeleteEmailTemplateAsync(Guid id);
        Task<bool> ActivateEmailTemplateAsync(Guid id);
        Task<bool> DeactivateEmailTemplateAsync(Guid id);
        Task<bool> IncrementSentCountAsync(Guid id);
        Task<bool> IncrementFailureCountAsync(Guid id);
        Task<EmailTemplateAnalyticsResponse?> GetEmailTemplateAnalyticsAsync(Guid id);
    }
}
