using CMS.WebApi.Services;

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
    /// Implementation of CMS integration service
    /// </summary>
    public class CmsIntegrationService : ICmsIntegrationService
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<CmsIntegrationService> _logger;

        public CmsIntegrationService(IDocumentService documentService, ILogger<CmsIntegrationService> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        public async Task<CmsDocument?> GetDocumentAsync(Guid documentId)
        {
            _logger.LogInformation("Retrieving document from CMS: {DocumentId}", documentId);

            var cmsDoc = await _documentService.RetrieveDocumentAsync(documentId);
            if (cmsDoc == null)
            {
                _logger.LogWarning("Document not found in CMS: {DocumentId}", documentId);
                return null;
            }

            // Get the file path and read the content
            var filePath = await _documentService.GetDocumentFilePathAsync(documentId);
            var fileContent = await File.ReadAllBytesAsync(filePath);

            _logger.LogInformation("Successfully retrieved CMS document: {FileName}, Size: {Size} bytes", cmsDoc.Name, fileContent.Length);

            return new CmsDocument
            {
                Id = cmsDoc.Id,
                FileName = cmsDoc.Name,
                FileContent = fileContent,
                ContentType = GetMimeType(cmsDoc.Name)
            };
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
