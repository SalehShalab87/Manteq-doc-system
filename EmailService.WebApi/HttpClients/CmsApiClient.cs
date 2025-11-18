using System.Text.Json;

namespace EmailService.WebApi.HttpClients
{
    /// <summary>
    /// Interface for CMS (Content Management Service) API client
    /// </summary>
    public interface ICmsApiClient
    {
        Task<CmsDocumentResponse?> GetDocumentAsync(Guid documentId);
        Task<string> GetDocumentFilePathAsync(Guid documentId);
        Task<byte[]> DownloadDocumentAsync(Guid documentId);
    }

    /// <summary>
    /// HTTP client for CMS API communication
    /// Handles document retrieval from CMS microservice
    /// </summary>
    public class CmsApiClient : ICmsApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CmsApiClient> _logger;

        public CmsApiClient(HttpClient httpClient, ILogger<CmsApiClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get document metadata by ID
        /// </summary>
        public async Task<CmsDocumentResponse?> GetDocumentAsync(Guid documentId)
        {
            try
            {
                _logger.LogInformation("🔍 CMS API: Getting document {DocumentId}", documentId);

                var response = await _httpClient.GetAsync($"/api/documents/{documentId}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("⚠️ CMS API: Document not found - {DocumentId}", documentId);
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var document = JsonSerializer.Deserialize<CmsDocumentResponse>(json, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("✅ CMS API: Document retrieved - {Name} ({Type})", 
                    document?.Name, document?.Type);

                return document;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ CMS API: HTTP error getting document {DocumentId}", documentId);
                throw new InvalidOperationException($"CMS API communication failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ CMS API: Unexpected error getting document {DocumentId}", documentId);
                throw;
            }
        }

        /// <summary>
        /// Get document file path by ID (for file system access)
        /// </summary>
        public async Task<string> GetDocumentFilePathAsync(Guid documentId)
        {
            try
            {
                _logger.LogInformation("🔍 CMS API: Getting document file path {DocumentId}", documentId);

                var response = await _httpClient.GetAsync($"/api/documents/{documentId}/filepath");
                response.EnsureSuccessStatusCode();

                var filePath = await response.Content.ReadAsStringAsync();
                // Remove quotes if present
                filePath = filePath.Trim('"');

                _logger.LogInformation("✅ CMS API: File path retrieved - {FilePath}", filePath);

                return filePath;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ CMS API: HTTP error getting file path for document {DocumentId}", documentId);
                throw new InvalidOperationException($"CMS API communication failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ CMS API: Unexpected error getting file path for document {DocumentId}", documentId);
                throw;
            }
        }

        /// <summary>
        /// Download document content by ID
        /// </summary>
        public async Task<byte[]> DownloadDocumentAsync(Guid documentId)
        {
            try
            {
                _logger.LogInformation("📥 CMS API: Downloading document {DocumentId}", documentId);

                var response = await _httpClient.GetAsync($"/api/documents/{documentId}/download");
                response.EnsureSuccessStatusCode();

                var fileBytes = await response.Content.ReadAsByteArrayAsync();

                _logger.LogInformation("✅ CMS API: Document downloaded - {DocumentId} ({Size} KB)", 
                    documentId, fileBytes.Length / 1024);

                return fileBytes;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ CMS API: HTTP error downloading document {DocumentId}", documentId);
                throw new InvalidOperationException($"CMS API communication failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ CMS API: Unexpected error downloading document {DocumentId}", documentId);
                throw;
            }
        }
    }

    #region DTOs

    /// <summary>
    /// CMS document response
    /// </summary>
    public class CmsDocumentResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; }
    }

    #endregion
}
