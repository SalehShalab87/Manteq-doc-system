using EmailService.WebApi.Models;

namespace EmailService.WebApi.Services
{
    /// <summary>
    /// Service for integrating with TMS (Template Management System)
    /// </summary>
    public interface ITmsIntegrationService
    {
        Task<GeneratedDocument> GenerateDocumentAsync(Guid templateId, Dictionary<string, string> propertyValues, TmsExportFormat exportFormat);
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
    /// Implementation of TMS integration service
    /// </summary>
    public class TmsIntegrationService : ITmsIntegrationService
    {
        private readonly TMS.WebApi.Services.IDocumentGenerationService _documentGenerationService;
        private readonly ILogger<TmsIntegrationService> _logger;

        public TmsIntegrationService(TMS.WebApi.Services.IDocumentGenerationService documentGenerationService, ILogger<TmsIntegrationService> logger)
        {
            _documentGenerationService = documentGenerationService;
            _logger = logger;
        }

        public async Task<GeneratedDocument> GenerateDocumentAsync(Guid templateId, Dictionary<string, string> propertyValues, TmsExportFormat exportFormat)
        {
            _logger.LogInformation("Generating document from TMS template: {TemplateId}, Format: {ExportFormat}", templateId, exportFormat);

            var tmsExportFormat = exportFormat switch
            {
                TmsExportFormat.Original => TMS.WebApi.Models.ExportFormat.Original,
                TmsExportFormat.Word => TMS.WebApi.Models.ExportFormat.Word,
                TmsExportFormat.Html => TMS.WebApi.Models.ExportFormat.Html,
                TmsExportFormat.EmailHtml => TMS.WebApi.Models.ExportFormat.EmailHtml,
                TmsExportFormat.Pdf => TMS.WebApi.Models.ExportFormat.Pdf,
                _ => TMS.WebApi.Models.ExportFormat.Original
            };

            var tmsRequest = new TMS.WebApi.Models.DocumentGenerationRequest
            {
                TemplateId = templateId,
                PropertyValues = propertyValues,
                ExportFormat = tmsExportFormat,
                GeneratedBy = "EmailService"
            };

            var result = await _documentGenerationService.GenerateDocumentAsync(tmsRequest);

            // If we get here, the generation was successful (it would throw on failure)
            if (result.GenerationId == Guid.Empty)
            {
                throw new InvalidOperationException($"Failed to generate document: {result.Message}");
            }

            // Get the file content
            var fileContent = await _documentGenerationService.DownloadGeneratedDocumentAsync(result.GenerationId);
            var content = string.Empty;

            // For EmailHtml, read content as string
            if (exportFormat == TmsExportFormat.EmailHtml)
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
