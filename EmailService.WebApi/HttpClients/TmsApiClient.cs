using System.Net.Http.Headers;
using System.Linq;
using System.Text;
using System.Text.Json;
using EmailService.WebApi.Models;

namespace EmailService.WebApi.HttpClients
{
    /// <summary>
    /// Interface for TMS (Template Management Service) API client
    /// </summary>
    public interface ITmsApiClient
    {
        Task<TmsDocumentGenerationResponse> GenerateDocumentAsync(TmsDocumentGenerationRequest request);
        Task<TmsGenerationResult> GenerateDocumentAndDownloadAsync(TmsDocumentGenerationRequest request);
        Task<byte[]> DownloadGeneratedDocumentAsync(Guid generationId);
    }

    /// <summary>
    /// HTTP client for TMS API communication
    /// Handles document generation requests to TMS microservice
    /// </summary>
    public class TmsApiClient : ITmsApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TmsApiClient> _logger;

        public TmsApiClient(HttpClient httpClient, ILogger<TmsApiClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Generate document from template with property values
        /// </summary>
        public async Task<TmsDocumentGenerationResponse> GenerateDocumentAsync(TmsDocumentGenerationRequest request)
        {
            try
            {
                _logger.LogInformation("🔍 TMS API: Generating document from template {TemplateId}, Format: {ExportFormat}", 
                    request.TemplateId, request.ExportFormat);

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/templates/generate", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TmsDocumentGenerationResponse>(responseJson, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("✅ TMS API: Document generated successfully - {FileName} ({FileSizeBytes} bytes)", 
                    result?.FileName, result?.FileSizeBytes);

                return result ?? throw new InvalidOperationException("Failed to deserialize TMS response");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ TMS API: HTTP error generating document from template {TemplateId}", request.TemplateId);
                throw new InvalidOperationException($"TMS API communication failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ TMS API: Unexpected error generating document from template {TemplateId}", request.TemplateId);
                throw;
            }
        }

        /// <summary>
        /// Generate document and request the TMS service to auto-download (stream) the file back in the same response.
        /// If TMS responds with JSON metadata instead, the Metadata field will be populated and FileBytes will be empty.
        /// </summary>
        public async Task<TmsGenerationResult> GenerateDocumentAndDownloadAsync(TmsDocumentGenerationRequest request)
        {
            try
            {
                _logger.LogInformation("🔄 TMS API: Generating document (auto-download) from template {TemplateId}, Format: {ExportFormat}",
                    request.TemplateId, request.ExportFormat);

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/templates/generate?autoDownload=true", content);
                response.EnsureSuccessStatusCode();

                var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;

                // If server returned JSON, parse as metadata
                if (mediaType.Contains("application/json"))
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var meta = JsonSerializer.Deserialize<TmsDocumentGenerationResponse>(responseJson, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    });

                    return new TmsGenerationResult { Metadata = meta ?? new TmsDocumentGenerationResponse(), FileBytes = Array.Empty<byte>() };
                }

                // Otherwise assume binary file stream
                var fileBytes = await response.Content.ReadAsByteArrayAsync();

                // Try to extract filename from content-disposition
                var contentDisposition = response.Content.Headers.ContentDisposition;
                var fileName = contentDisposition?.FileNameStar ?? contentDisposition?.FileName ?? "generated_document";
                fileName = fileName.Trim('"');

                // Try to extract generation id header if provided
                Guid generationId = Guid.Empty;
                if (response.Headers.TryGetValues("X-Generation-Id", out var values))
                {
                    var val = values.FirstOrDefault();
                    if (!string.IsNullOrEmpty(val))
                        Guid.TryParse(val, out generationId);
                }

                var metaResp = new TmsDocumentGenerationResponse
                {
                    GenerationId = generationId,
                    FileName = fileName,
                    FileSizeBytes = fileBytes.Length,
                    GeneratedAt = DateTime.UtcNow,
                    DownloadUrl = string.Empty,
                    Message = "File returned in response"
                };

                _logger.LogInformation("✅ TMS API: Received auto-downloaded file - {FileName} ({Size} KB)", fileName, fileBytes.Length / 1024);

                return new TmsGenerationResult { Metadata = metaResp, FileBytes = fileBytes };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ TMS API: HTTP error generating document (auto-download) from template {TemplateId}", request.TemplateId);
                throw new InvalidOperationException($"TMS API communication failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ TMS API: Unexpected error generating document (auto-download) from template {TemplateId}", request.TemplateId);
                throw;
            }
        }

        /// <summary>
        /// Download generated document by generation ID
        /// </summary>
        public async Task<byte[]> DownloadGeneratedDocumentAsync(Guid generationId)
        {
            try
            {
                _logger.LogInformation("📥 TMS API: Downloading generated document {GenerationId}", generationId);

                var response = await _httpClient.GetAsync($"/api/templates/download/{generationId}");
                response.EnsureSuccessStatusCode();

                var fileBytes = await response.Content.ReadAsByteArrayAsync();

                _logger.LogInformation("✅ TMS API: Downloaded document successfully - {GenerationId} ({Size} KB)", 
                    generationId, fileBytes.Length / 1024);

                return fileBytes;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ TMS API: HTTP error downloading document {GenerationId}", generationId);
                throw new InvalidOperationException($"TMS API communication failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ TMS API: Unexpected error downloading document {GenerationId}", generationId);
                throw;
            }
        }
    }

    #region DTOs

    /// <summary>
    /// Request to generate document from TMS template
    /// </summary>
    public class TmsDocumentGenerationRequest
    {
        public Guid TemplateId { get; set; }
        public Dictionary<string, string> PropertyValues { get; set; } = new();
        public TmsExportFormat ExportFormat { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response from TMS document generation
    /// </summary>
    public class TmsDocumentGenerationResponse
    {
        public Guid GenerationId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result returned when requesting generation with auto-download
    /// Contains both metadata (if available) and the raw file bytes (if streamed)
    /// </summary>
    public class TmsGenerationResult
    {
        public TmsDocumentGenerationResponse Metadata { get; set; } = new TmsDocumentGenerationResponse();
        public byte[] FileBytes { get; set; } = Array.Empty<byte>();
    }

    #endregion
}
