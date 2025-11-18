using EmailService.WebApi.Models;
using System.Text;
using System.Text.Json;

namespace EmailService.WebApi.Services
{
    /// <summary>
    /// Service for integrating with CMS Email Template APIs
    /// </summary>
    public interface IEmailTemplateService
    {
        Task<EmailTemplateDto?> GetEmailTemplateAsync(Guid templateId);
        Task<string> GetCustomTemplateContentAsync(Guid templateId);
        Task<List<EmailTemplateAttachmentDto>> GetTemplateAttachmentsAsync(Guid templateId);
        Task<byte[]> GetCustomAttachmentAsync(Guid templateId, int attachmentIndex);
    }

    /// <summary>
    /// Implementation of Email Template integration service using CMS HTTP client
    /// </summary>
    public class EmailTemplateIntegrationService : IEmailTemplateService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EmailTemplateIntegrationService> _logger;

        public EmailTemplateIntegrationService(
            IHttpClientFactory httpClientFactory,
            ILogger<EmailTemplateIntegrationService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("CmsApi");
            _logger = logger;
        }

        public async Task<EmailTemplateDto?> GetEmailTemplateAsync(Guid templateId)
        {
            _logger.LogInformation("🔍 CMS API: Fetching email template {TemplateId}", templateId);

            try
            {
                var response = await _httpClient.GetAsync($"/api/email-templates/{templateId}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("⚠️ CMS API: Email template not found - {TemplateId}", templateId);
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var template = JsonSerializer.Deserialize<EmailTemplateDto>(json, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("✅ CMS API: Email template retrieved - {Name}", template?.Name);
                return template;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ CMS API: HTTP error fetching email template {TemplateId}", templateId);
                throw new InvalidOperationException($"CMS API communication failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ CMS API: Unexpected error fetching email template {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<string> GetCustomTemplateContentAsync(Guid templateId)
        {
            _logger.LogInformation("🔍 CMS API: Fetching custom template content {TemplateId}", templateId);

            try
            {
                var response = await _httpClient.GetAsync($"/api/email-templates/{templateId}/custom-template");
                response.EnsureSuccessStatusCode();

                var htmlBytes = await response.Content.ReadAsByteArrayAsync();
                var html = Encoding.UTF8.GetString(htmlBytes);
                
                _logger.LogInformation("✅ CMS API: Custom template content retrieved ({Length} bytes)", htmlBytes.Length);
                return html;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ CMS API: HTTP error fetching custom template {TemplateId}", templateId);
                throw new InvalidOperationException($"CMS API communication failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ CMS API: Unexpected error fetching custom template {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<List<EmailTemplateAttachmentDto>> GetTemplateAttachmentsAsync(Guid templateId)
        {
            _logger.LogInformation("🔍 CMS API: Fetching template attachments {TemplateId}", templateId);

            try
            {
                var response = await _httpClient.GetAsync($"/api/email-templates/{templateId}/attachments");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var attachments = JsonSerializer.Deserialize<List<EmailTemplateAttachmentDto>>(json, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                }) ?? new List<EmailTemplateAttachmentDto>();
                
                _logger.LogInformation("✅ CMS API: Retrieved {Count} template attachments", attachments.Count);
                return attachments;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ CMS API: HTTP error fetching attachments {TemplateId}", templateId);
                throw new InvalidOperationException($"CMS API communication failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ CMS API: Unexpected error fetching attachments {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<byte[]> GetCustomAttachmentAsync(Guid templateId, int attachmentIndex)
        {
            _logger.LogInformation("📥 CMS API: Fetching custom attachment TemplateId={TemplateId}, Index={Index}", 
                templateId, attachmentIndex);

            try
            {
                var response = await _httpClient.GetAsync(
                    $"/api/email-templates/{templateId}/attachments/{attachmentIndex}/download");
                response.EnsureSuccessStatusCode();

                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                
                _logger.LogInformation("✅ CMS API: Custom attachment retrieved ({Length} bytes)", fileBytes.Length);
                return fileBytes;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ CMS API: HTTP error fetching attachment TemplateId={TemplateId}, Index={Index}", 
                    templateId, attachmentIndex);
                throw new InvalidOperationException($"CMS API communication failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ CMS API: Unexpected error fetching attachment TemplateId={TemplateId}, Index={Index}", 
                    templateId, attachmentIndex);
                throw;
            }
        }
    }
}
