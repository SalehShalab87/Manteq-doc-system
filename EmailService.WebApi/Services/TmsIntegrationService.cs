using EmailService.WebApi.Models;
using EmailService.WebApi.HttpClients;

namespace EmailService.WebApi.Services
{
    /// <summary>
    /// Service for integrating with TMS (Template Management System)
    /// </summary>
    public interface ITmsIntegrationService
    {
        Task<GeneratedDocument> GenerateDocumentAsync(Guid templateId, Dictionary<string, string> propertyValues, Models.TmsExportFormat exportFormat);
        Task CleanupGeneratedDocumentAsync(Guid generationId);
    }

    /// <summary>
    /// Represents a document generated from TMS
    /// </summary>
    public class GeneratedDocument
    {
        public Guid GenerationId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public byte[] FileContent { get; set; } = Array.Empty<byte>();
        public string Content { get; set; } = string.Empty; // For EmailHtml format
    }

    /// <summary>
    /// Implementation of TMS integration service using HTTP client
    /// </summary>
    public class TmsIntegrationService : ITmsIntegrationService
    {
        private readonly ITmsApiClient _tmsApiClient;
        private readonly ILogger<TmsIntegrationService> _logger;

        public TmsIntegrationService(ITmsApiClient tmsApiClient, ILogger<TmsIntegrationService> logger)
        {
            _tmsApiClient = tmsApiClient;
            _logger = logger;
        }

        public async Task<GeneratedDocument> GenerateDocumentAsync(Guid templateId, Dictionary<string, string> propertyValues, Models.TmsExportFormat exportFormat)
        {
            _logger.LogInformation("Generating document from TMS template: {TemplateId}, Format: {ExportFormat}", templateId, exportFormat);

            var request = new TmsDocumentGenerationRequest
            {
                TemplateId = templateId,
                PropertyValues = propertyValues,
                ExportFormat = exportFormat,
                GeneratedBy = "EmailService"
            };

            // Request generation and attempt to receive the file stream in the same response
            var genResult = await _tmsApiClient.GenerateDocumentAndDownloadAsync(request);
            var result = genResult.Metadata;

            // If the server streamed the file back, use it; otherwise, fall back to separate download
            byte[] fileContent = genResult.FileBytes ?? Array.Empty<byte>();
            if (fileContent.Length == 0)
            {
                if (result.GenerationId == Guid.Empty)
                {
                    throw new InvalidOperationException($"Failed to generate document: {result.Message}");
                }

                fileContent = await _tmsApiClient.DownloadGeneratedDocumentAsync(result.GenerationId);
            }
            var content = string.Empty;

            // For EmailHtml, read content as string
            if (exportFormat == Models.TmsExportFormat.EmailHtml)
            {
                content = System.Text.Encoding.UTF8.GetString(fileContent);
            }

            _logger.LogInformation("Successfully generated document: {FileName}, Size: {Size} bytes", result.FileName, fileContent.Length);

            return new GeneratedDocument
            {
                GenerationId = result.GenerationId,
                FileName = result.FileName,
                FileContent = fileContent,
                Content = content
            };
        }

        public async Task CleanupGeneratedDocumentAsync(Guid generationId)
        {
            _logger.LogDebug("Cleaning up generated document: {GenerationId}", generationId);
            // The TMS service handles cleanup automatically through its timer
            // This method is here for interface compatibility
            await Task.CompletedTask;
        }
    }
}
