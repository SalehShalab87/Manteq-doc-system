using CMS.WebApi.Models;

namespace CMS.WebApi.Services
{
    public interface IEmailTemplateService
    {
        Task<EmailTemplateResponse> CreateEmailTemplateAsync(CreateEmailTemplateRequest request, string createdBy);
        Task<EmailTemplateResponse?> GetEmailTemplateByIdAsync(Guid id);
        Task<List<EmailTemplateResponse>> GetAllEmailTemplatesAsync(string? name = null,bool? isActive = null, string? category = null);
        Task<EmailTemplateResponse?> UpdateEmailTemplateAsync(Guid id, UpdateEmailTemplateRequest request);
        Task<bool> DeleteEmailTemplateAsync(Guid id);
        Task<bool> ActivateEmailTemplateAsync(Guid id);
        Task<bool> DeactivateEmailTemplateAsync(Guid id);
        Task<bool> IncrementSentCountAsync(Guid id);
        Task<bool> IncrementFailureCountAsync(Guid id);
        Task<EmailTemplateAnalyticsResponse?> GetEmailTemplateAnalyticsAsync(Guid id);
        
        // Additional methods for template management
        Task<EmailTemplate?> GetTemplateByIdAsync(Guid id);
        Task<List<EmailTemplate>> GetAllTemplatesAsync();
        Task<EmailTemplate> CreateTemplateAsync(CreateEmailTemplateRequest request);
        Task<EmailTemplate?> UpdateTemplateAsync(Guid id, CreateEmailTemplateRequest request);
        Task<bool> DeleteTemplateAsync(Guid id);
        Task<List<EmailTemplateAttachment>> GetTemplateAttachmentsAsync(Guid templateId);
        
        // Set the stored custom template file path after successful upload
        Task<bool> SetCustomTemplateFilePathAsync(Guid templateId, string filePath, string updatedBy);
    }
}
