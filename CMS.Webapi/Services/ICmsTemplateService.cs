using CMS.WebApi.Models;

namespace CMS.WebApi.Services
{
    public interface ICmsTemplateService
    {
        Task<Template> CreateTemplateAsync(Template template);
        Task<Template?> GetTemplateByIdAsync(Guid id);
        Task<List<Template>> GetAllTemplatesAsync();
        Task<List<Template>> GetActiveTemplatesAsync();
        Task<List<Template>> GetTemplatesByCategoryAsync(string category);
        Task<Template?> UpdateTemplateAsync(Guid id, Template template);
        Task<bool> DeleteTemplateAsync(Guid id);
        Task<bool> DeactivateTemplateAsync(Guid id);
        Task<bool> ActivateTemplateAsync(Guid id);
    }
}
