using System.Net.Http.Json;
using System.Text.Json;

namespace TMS.WebApi.HttpClients
{
    public interface ICmsApiClient
    {
        // Document Operations
        Task<DocumentResponse?> GetDocumentAsync(Guid documentId);
        Task<DocumentResponse> RegisterDocumentAsync(RegisterDocumentRequest request);
        Task<byte[]> DownloadDocumentAsync(Guid documentId);
        Task<List<DocumentResponse>> GetAllDocumentsAsync();
        Task ActivateDocumentAsync(Guid documentId);
        Task DeactivateDocumentAsync(Guid documentId);
        Task<string> GetDocumentFilePathAsync(Guid documentId);

        // Template Operations
        Task<TemplateResponse> CreateTemplateAsync(CreateTemplateRequest request);
        Task<TemplateResponse?> GetTemplateByIdAsync(Guid templateId);
        Task<List<TemplateResponse>> GetAllTemplatesAsync();
        Task<TemplateResponse?> UpdateTemplateAsync(Guid id, UpdateTemplateRequest request);
        Task<bool> DeleteTemplateAsync(Guid id);
        Task<bool> ActivateTemplateAsync(Guid id);
        Task<bool> DeactivateTemplateAsync(Guid id);
        Task<bool> IncrementSuccessCountAsync(Guid id);
        Task<bool> IncrementFailureCountAsync(Guid id);
    }

    public class CmsApiClient : ICmsApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CmsApiClient> _logger;

        public CmsApiClient(HttpClient httpClient, ILogger<CmsApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<DocumentResponse?> GetDocumentAsync(Guid documentId)
        {
            try
            {
                _logger.LogInformation("🔍 TMS calling CMS API: GET /api/documents/{DocumentId}", documentId);
                
                var response = await _httpClient.GetAsync($"/api/documents/{documentId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("⚠️ CMS API returned {StatusCode} for document {DocumentId}", 
                        response.StatusCode, documentId);
                    return null;
                }

                var document = await response.Content.ReadFromJsonAsync<DocumentResponse>();
                _logger.LogInformation("✅ TMS retrieved document {DocumentId} from CMS", documentId);
                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calling CMS API to get document {DocumentId}", documentId);
                throw new InvalidOperationException($"Failed to retrieve document from CMS: {ex.Message}", ex);
            }
        }

        public async Task<DocumentResponse> RegisterDocumentAsync(RegisterDocumentRequest request)
        {
            try
            {
                _logger.LogInformation("📝 TMS calling CMS API: POST /api/documents/register - {Name}", request.Name);

                using var content = new MultipartFormDataContent();
                
                // Add file content using the CMS expected field name 'Content'
                var fileContent = new ByteArrayContent(request.FileContent);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.MimeType);
                content.Add(fileContent, "Content", request.FileName);

                // Add required metadata fields CMS expects
                content.Add(new StringContent(request.Name), "Name");
                content.Add(new StringContent(request.Type ?? ""), "Type");

                // Create HttpRequestMessage so we can set headers like X-SME-UserId
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/documents/register")
                {
                    Content = content
                };

                // CMS expects created-by via header, not form field
                if (!string.IsNullOrEmpty(request.CreatedBy))
                {
                    httpRequest.Headers.Add("X-SME-UserId", request.CreatedBy);
                }

                var response = await _httpClient.SendAsync(httpRequest);
                response.EnsureSuccessStatusCode();

                var document = await response.Content.ReadFromJsonAsync<DocumentResponse>();
                _logger.LogInformation("✅ TMS registered document {DocumentId} in CMS", document?.Id);
                
                return document ?? throw new InvalidOperationException("CMS returned null document");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calling CMS API to register document");
                throw new InvalidOperationException($"Failed to register document in CMS: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> DownloadDocumentAsync(Guid documentId)
        {
            try
            {
                _logger.LogInformation("📥 TMS calling CMS API: GET /api/documents/{DocumentId}/download", documentId);
                
                var response = await _httpClient.GetAsync($"/api/documents/{documentId}/download");
                response.EnsureSuccessStatusCode();

                var bytes = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation("✅ TMS downloaded document {DocumentId} ({Size} bytes)", documentId, bytes.Length);
                
                return bytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calling CMS API to download document {DocumentId}", documentId);
                throw new InvalidOperationException($"Failed to download document from CMS: {ex.Message}", ex);
            }
        }

        public async Task<List<DocumentResponse>> GetAllDocumentsAsync()
        {
            try
            {
                _logger.LogInformation("📋 TMS calling CMS API: GET /api/documents");
                
                var response = await _httpClient.GetAsync("/api/documents");
                response.EnsureSuccessStatusCode();

                var documents = await response.Content.ReadFromJsonAsync<List<DocumentResponse>>();
                _logger.LogInformation("✅ TMS retrieved {Count} documents from CMS", documents?.Count ?? 0);
                
                return documents ?? new List<DocumentResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calling CMS API to get all documents");
                throw new InvalidOperationException($"Failed to retrieve documents from CMS: {ex.Message}", ex);
            }
        }

        public async Task ActivateDocumentAsync(Guid documentId)
        {
            try
            {
                _logger.LogInformation("🟢 TMS calling CMS API: POST /api/documents/{DocumentId}/activate", documentId);
                
                var response = await _httpClient.PostAsync($"/api/documents/{documentId}/activate", null);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("✅ TMS activated document {DocumentId} in CMS", documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calling CMS API to activate document {DocumentId}", documentId);
                throw new InvalidOperationException($"Failed to activate document in CMS: {ex.Message}", ex);
            }
        }

        public async Task DeactivateDocumentAsync(Guid documentId)
        {
            try
            {
                _logger.LogInformation("🔴 TMS calling CMS API: POST /api/documents/{DocumentId}/deactivate", documentId);
                
                var response = await _httpClient.PostAsync($"/api/documents/{documentId}/deactivate", null);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("✅ TMS deactivated document {DocumentId} in CMS", documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calling CMS API to deactivate document {DocumentId}", documentId);
                throw new InvalidOperationException($"Failed to deactivate document in CMS: {ex.Message}", ex);
            }
        }

        public async Task<string> GetDocumentFilePathAsync(Guid documentId)
        {
            try
            {
                var document = await GetDocumentAsync(documentId);
                if (document == null)
                    throw new InvalidOperationException($"Document {documentId} not found");

                // If CMS returned a file path that is accessible from this service, use it
                if (!string.IsNullOrEmpty(document.FilePath) && System.IO.File.Exists(document.FilePath))
                {
                    return document.FilePath;
                }

                // Fallback: download the document bytes from CMS and save to a temp file
                _logger.LogInformation("📥 Falling back to HTTP download for document {DocumentId}", documentId);
                var bytes = await DownloadDocumentAsync(documentId);

                var extension = !string.IsNullOrEmpty(document.Extension) ? document.Extension : System.IO.Path.GetExtension(document.Name);
                if (string.IsNullOrEmpty(extension)) extension = ".bin";
                if (!extension.StartsWith(".")) extension = "." + extension.TrimStart('.');

                var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{System.Guid.NewGuid()}{extension}");
                await System.IO.File.WriteAllBytesAsync(tempFile, bytes);
                _logger.LogInformation("✅ Wrote downloaded document to temp file: {TempFile}", tempFile);

                return tempFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting document file path for {DocumentId}", documentId);
                throw;
            }
        }

        // Template Operations
        public async Task<TemplateResponse> CreateTemplateAsync(CreateTemplateRequest request)
        {
            try
            {
                _logger.LogInformation("📝 TMS calling CMS API: POST /api/templates - {Name}", request.Name);

                var response = await _httpClient.PostAsJsonAsync("/api/templates", request);
                response.EnsureSuccessStatusCode();

                var template = await response.Content.ReadFromJsonAsync<TemplateResponse>();
                _logger.LogInformation("✅ TMS created template {TemplateId} in CMS", template?.Id);

                return template ?? throw new InvalidOperationException("CMS returned null template");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calling CMS API to create template");
                throw new InvalidOperationException($"Failed to create template in CMS: {ex.Message}", ex);
            }
        }

        public async Task<TemplateResponse?> GetTemplateByIdAsync(Guid templateId)
        {
            try
            {
                _logger.LogInformation("🔍 TMS calling CMS API: GET /api/templates/{TemplateId}", templateId);

                var response = await _httpClient.GetAsync($"/api/templates/{templateId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("⚠️ CMS API returned {StatusCode} for template {TemplateId}",
                        response.StatusCode, templateId);
                    return null;
                }

                var template = await response.Content.ReadFromJsonAsync<TemplateResponse>();
                _logger.LogInformation("✅ TMS retrieved template {TemplateId} from CMS", templateId);
                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calling CMS API to get template {TemplateId}", templateId);
                throw new InvalidOperationException($"Failed to retrieve template from CMS: {ex.Message}", ex);
            }
        }

        public async Task<List<TemplateResponse>> GetAllTemplatesAsync()
        {
            try
            {
                _logger.LogInformation("📋 TMS calling CMS API: GET /api/templates");

                var response = await _httpClient.GetAsync("/api/templates");
                response.EnsureSuccessStatusCode();

                var templates = await response.Content.ReadFromJsonAsync<List<TemplateResponse>>();
                _logger.LogInformation("✅ TMS retrieved {Count} templates from CMS", templates?.Count ?? 0);

                return templates ?? new List<TemplateResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calling CMS API to get all templates");
                throw new InvalidOperationException($"Failed to retrieve templates from CMS: {ex.Message}", ex);
            }
        }

        public async Task<TemplateResponse?> UpdateTemplateAsync(Guid id, UpdateTemplateRequest request)
        {
            try
            {
                _logger.LogInformation("📝 TMS calling CMS API: PUT /api/templates/{TemplateId}", id);

                var response = await _httpClient.PutAsJsonAsync($"/api/templates/{id}", request);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("⚠️ CMS API returned {StatusCode} for template update {TemplateId}",
                        response.StatusCode, id);
                    return null;
                }

                var template = await response.Content.ReadFromJsonAsync<TemplateResponse>();
                _logger.LogInformation("✅ TMS updated template {TemplateId} in CMS", id);
                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calling CMS API to update template {TemplateId}", id);
                throw new InvalidOperationException($"Failed to update template in CMS: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteTemplateAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("🗑️ TMS calling CMS API: DELETE /api/templates/{TemplateId}", id);

                var response = await _httpClient.DeleteAsync($"/api/templates/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ TMS deleted template {TemplateId} in CMS", id);
                    return true;
                }

                _logger.LogWarning("⚠️ CMS API returned {StatusCode} for template delete {TemplateId}",
                    response.StatusCode, id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calling CMS API to delete template {TemplateId}", id);
                throw new InvalidOperationException($"Failed to delete template in CMS: {ex.Message}", ex);
            }
        }

        public async Task<bool> ActivateTemplateAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("🟢 TMS calling CMS API: POST /api/templates/{TemplateId}/activate", id);

                var response = await _httpClient.PostAsync($"/api/templates/{id}/activate", null);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ TMS activated template {TemplateId} in CMS", id);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calling CMS API to activate template {TemplateId}", id);
                return false;
            }
        }

        public async Task<bool> DeactivateTemplateAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("🔴 TMS calling CMS API: POST /api/templates/{TemplateId}/deactivate", id);

                var response = await _httpClient.PostAsync($"/api/templates/{id}/deactivate", null);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ TMS deactivated template {TemplateId} in CMS", id);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calling CMS API to deactivate template {TemplateId}", id);
                return false;
            }
        }

        public async Task<bool> IncrementSuccessCountAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("📈 TMS calling CMS API: POST /api/templates/{TemplateId}/increment-success", id);

                var response = await _httpClient.PostAsync($"/api/templates/{id}/increment-success", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calling CMS API to increment success count {TemplateId}", id);
                return false;
            }
        }

        public async Task<bool> IncrementFailureCountAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("📉 TMS calling CMS API: POST /api/templates/{TemplateId}/increment-failure", id);

                var response = await _httpClient.PostAsync($"/api/templates/{id}/increment-failure", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calling CMS API to increment failure count {TemplateId}", id);
                return false;
            }
        }
    }

    // DTOs matching CMS API contracts
    public class DocumentResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime CreationDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public string? Category { get; set; }
    }

    public class RegisterDocumentRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? Category { get; set; }
        public byte[] FileContent { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
    }

    // Template DTOs
    public class TemplateResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public Guid CmsDocumentId { get; set; }
        public List<string> Placeholders { get; set; } = new();
        public int TemplateType { get; set; }
        public int DefaultExportFormat { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
    }

    public class CreateTemplateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public Guid CmsDocumentId { get; set; }
        public List<string> Placeholders { get; set; } = new();
        public int TemplateType { get; set; }
        public int DefaultExportFormat { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateTemplateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public Guid CmsDocumentId { get; set; }
        public List<string> Placeholders { get; set; } = new();
        public int TemplateType { get; set; }
        public int DefaultExportFormat { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsActive { get; set; }
    }
}
