using EmailService.WebApi.HttpClients;

namespace EmailService.WebApi.Services
{
    /// <summary>
    /// Service for integrating with CMS (Content Management System)
    /// </summary>
    public interface ICmsIntegrationService
    {
        Task<CmsDocument?> GetDocumentAsync(Guid documentId);
    }

    /// <summary>
    /// Represents a document from CMS
    /// </summary>
    public class CmsDocument
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public byte[] FileContent { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Implementation of CMS integration service using HTTP client
    /// </summary>
    public class CmsIntegrationService : ICmsIntegrationService
    {
        private readonly ICmsApiClient _cmsApiClient;
        private readonly ILogger<CmsIntegrationService> _logger;

        public CmsIntegrationService(ICmsApiClient cmsApiClient, ILogger<CmsIntegrationService> logger)
        {
            _cmsApiClient = cmsApiClient;
            _logger = logger;
        }

        public async Task<CmsDocument?> GetDocumentAsync(Guid documentId)
        {
            _logger.LogInformation("Retrieving document from CMS: {DocumentId}", documentId);

            var cmsDoc = await _cmsApiClient.GetDocumentAsync(documentId);
            if (cmsDoc == null)
            {
                _logger.LogWarning("Document not found in CMS: {DocumentId}", documentId);
                return null;
            }

            // Get the file content via HTTP
            var fileContent = await _cmsApiClient.DownloadDocumentAsync(documentId);

            _logger.LogInformation("Successfully retrieved CMS document: {FileName}, Size: {Size} bytes", cmsDoc.Name, fileContent.Length);

            // Construct proper filename with extension
            var fileName = GetFileNameWithExtension(cmsDoc.Name, cmsDoc.Type);

            return new CmsDocument
            {
                Id = cmsDoc.Id,
                FileName = fileName,
                FileContent = fileContent,
                ContentType = GetMimeType(fileName)
            };
        }

        private static string GetFileNameWithExtension(string name, string type)
        {
            // If name already has an extension, use it as-is
            if (Path.HasExtension(name))
            {
                return name;
            }

            // Fallback: map document type to extension
            var extension = type.ToLowerInvariant() switch
            {
                "word" => ".docx",
                "excel" => ".xlsx",
                "powerpoint" => ".pptx",
                "pdf" => ".pdf",
                "text" => ".txt",
                "html" => ".html",
                _ => ".bin" // Generic binary extension
            };

            return name + extension;
        }

        private static string GetMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".doc" => "application/msword",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xls" => "application/vnd.ms-excel",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".html" => "text/html",
                ".htm" => "text/html",
                ".txt" => "text/plain",
                ".rtf" => "application/rtf",
                ".zip" => "application/zip",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };
        }
    }
}
