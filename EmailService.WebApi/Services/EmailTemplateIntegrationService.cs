using EmailService.WebApi.Models;
using CMS.WebApi.Models;
using System.Text;

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
    /// Implementation of Email Template integration service
    /// </summary>
    public class EmailTemplateIntegrationService : IEmailTemplateService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EmailTemplateIntegrationService> _logger;
        private readonly string _cmsApiBaseUrl;

        public EmailTemplateIntegrationService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<EmailTemplateIntegrationService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("CmsApi");
            _logger = logger;
            _cmsApiBaseUrl = Environment.GetEnvironmentVariable("CMS_BASE_URL") 
                ?? Environment.GetEnvironmentVariable("CMS_API_URL") 
                ?? configuration["CmsApi:BaseUrl"] 
                ?? "http://localhost:5000";

            _httpClient.BaseAddress = new Uri(_cmsApiBaseUrl);
            _logger.LogInformation("🔗 EmailTemplateIntegrationService using CMS API: {CmsApiUrl}", _cmsApiBaseUrl);
        }

        public async Task<EmailTemplateDto?> GetEmailTemplateAsync(Guid templateId)
        {
            _logger.LogInformation("Fetching email template from CMS: {TemplateId}", templateId);

            try
            {
                var response = await _httpClient.GetAsync($"/api/email-templates/{templateId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning("Email template not found: {TemplateId}", templateId);
                        return null;
                    }

                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Failed to fetch template: {response.StatusCode}, {errorContent}");
                }

                var template = await response.Content.ReadFromJsonAsync<EmailTemplateDto>();
                _logger.LogInformation("Successfully fetched email template: {TemplateName}", template?.Name);
                
                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching email template: {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<string> GetCustomTemplateContentAsync(Guid templateId)
        {
            _logger.LogInformation("Fetching custom template content from CMS: {TemplateId}", templateId);

            try
            {
                var response = await _httpClient.GetAsync($"/api/email-templates/{templateId}/custom-template");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Failed to fetch custom template content: {response.StatusCode}, {errorContent}");
                }

                var htmlBytes = await response.Content.ReadAsByteArrayAsync();
                var html = Encoding.UTF8.GetString(htmlBytes);
                
                _logger.LogInformation("Successfully fetched custom template content: {Length} bytes", htmlBytes.Length);
                
                return html;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching custom template content: {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<List<EmailTemplateAttachmentDto>> GetTemplateAttachmentsAsync(Guid templateId)
        {
            _logger.LogInformation("Fetching template attachments from CMS: {TemplateId}", templateId);

            try
            {
                var response = await _httpClient.GetAsync($"/api/email-templates/{templateId}/attachments");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Failed to fetch template attachments: {response.StatusCode}, {errorContent}");
                }

                var attachments = await response.Content.ReadFromJsonAsync<List<EmailTemplateAttachmentDto>>() 
                    ?? new List<EmailTemplateAttachmentDto>();
                
                _logger.LogInformation("Successfully fetched {Count} template attachments", attachments.Count);
                
                return attachments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching template attachments: {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<byte[]> GetCustomAttachmentAsync(Guid templateId, int attachmentIndex)
        {
            _logger.LogInformation("Fetching custom attachment from CMS: TemplateId={TemplateId}, Index={Index}", templateId, attachmentIndex);

            try
            {
                var response = await _httpClient.GetAsync($"/api/email-templates/{templateId}/attachments/{attachmentIndex}/download");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Failed to fetch custom attachment: {response.StatusCode}, {errorContent}");
                }

                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                
                _logger.LogInformation("Successfully fetched custom attachment: {Length} bytes", fileBytes.Length);
                
                return fileBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching custom attachment: TemplateId={TemplateId}, Index={Index}", templateId, attachmentIndex);
                throw;
            }
        }
    }

    /// <summary>
    /// DTO for email template from CMS
    /// </summary>
    public class EmailTemplateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public EmailBodySourceType BodySourceType { get; set; }
        public string? PlainTextContent { get; set; }
        public Guid? TmsTemplateId { get; set; }
        public string? CustomTemplateFilePath { get; set; }
    }

    /// <summary>
    /// DTO for email template attachment from CMS
    /// </summary>
    public class EmailTemplateAttachmentDto
    {
        public Guid Id { get; set; }
        public Guid EmailTemplateId { get; set; }
        public AttachmentSourceType SourceType { get; set; }
        public Guid? CmsDocumentId { get; set; }
        public Guid? TmsTemplateId { get; set; }
        public EmailService.WebApi.Models.TmsExportFormat? TmsExportFormat { get; set; }
        public string? CustomFilePath { get; set; }
        public string? CustomFileName { get; set; }
        public int DisplayOrder { get; set; }
    }
}
