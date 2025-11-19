using CMS.WebApi.Models;
using Microsoft.EntityFrameworkCore;
using CMS.WebApi.Data;

namespace CMS.WebApi.Services
{
    public interface IEmailTemplateFileService
    {
        Task<string> SaveCustomTemplateAsync(IFormFile file, Guid templateId);
        Task<string> SaveCustomAttachmentAsync(IFormFile file);
        Task<bool> DeleteCustomTemplateAsync(string filePath);
        Task<bool> DeleteCustomAttachmentAsync(string filePath);
        Task<byte[]?> GetFileAsync(string filePath);
        Task<byte[]> GetCustomTemplateAsync(Guid templateId);
        Task<(byte[] FileBytes, string FileName, string ContentType)> GetCustomAttachmentAsync(Guid templateId, int attachmentIndex);
    }

    public class EmailTemplateFileService : IEmailTemplateFileService
    {
        private readonly string _emailTemplatesPath;
        private readonly string _emailAttachmentsPath;
        private readonly ILogger<EmailTemplateFileService> _logger;

        public EmailTemplateFileService(
            IConfiguration configuration,
            ILogger<EmailTemplateFileService> logger)
        {
            _logger = logger;

            var baseStoragePath = configuration["FileStorage:Path"] ?? 
                Path.Combine(Directory.GetCurrentDirectory(), "Storage");

            _emailTemplatesPath = Path.Combine(baseStoragePath, "EmailTemplates");
            _emailAttachmentsPath = Path.Combine(baseStoragePath, "EmailAttachments");

            // Ensure directories exist
            EnsureDirectoriesExist();
        }

        private void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(_emailTemplatesPath))
            {
                Directory.CreateDirectory(_emailTemplatesPath);
                _logger.LogInformation("Created email templates directory: {Path}", _emailTemplatesPath);
            }

            if (!Directory.Exists(_emailAttachmentsPath))
            {
                Directory.CreateDirectory(_emailAttachmentsPath);
                _logger.LogInformation("Created email attachments directory: {Path}", _emailAttachmentsPath);
            }
        }

        public async Task<string> SaveCustomTemplateAsync(IFormFile file, Guid templateId)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File is empty");

                var extension = Path.GetExtension(file.FileName).ToLower();
                // Accept only MHTML variants for custom email templates
                var allowedExtensions = new[] { ".mht", ".mhtml" };
                if (!allowedExtensions.Contains(extension))
                    throw new ArgumentException("Only .mht and .mhtml files are allowed");

                if (file.Length > 5 * 1024 * 1024) // 5MB limit
                    throw new ArgumentException("File size exceeds 5MB limit");

                // Generate unique filename
                var fileName = $"{templateId}_template{extension}";
                var filePath = Path.Combine(_emailTemplatesPath, fileName);

                // Delete existing file if it exists
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Deleted existing template file: {FilePath}", filePath);
                }

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("Saved custom template: {FilePath}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving custom template for template {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<string> SaveCustomAttachmentAsync(IFormFile file)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File is empty");

                if (file.Length > 10 * 1024 * 1024) // 10MB limit
                    throw new ArgumentException("File size exceeds 10MB limit");

                // Validate extension
                var extension = Path.GetExtension(file.FileName).ToLower();
                var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx", ".pptx", ".doc", ".xls", ".ppt", ".txt", ".png", ".jpg", ".jpeg", ".gif" };
                if (!allowedExtensions.Contains(extension))
                    throw new ArgumentException($"File type {extension} is not allowed");

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(_emailAttachmentsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("Saved custom attachment: {FilePath}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving custom attachment");
                throw;
            }
        }

        public async Task<bool> DeleteCustomTemplateAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    _logger.LogInformation("Deleted custom template: {FilePath}", filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting custom template: {FilePath}", filePath);
                return false;
            }
        }

        public async Task<bool> DeleteCustomAttachmentAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    _logger.LogInformation("Deleted custom attachment: {FilePath}", filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting custom attachment: {FilePath}", filePath);
                return false;
            }
        }

        public async Task<byte[]?> GetFileAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    return await File.ReadAllBytesAsync(filePath);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file: {FilePath}", filePath);
                return null;
            }
        }

        public async Task<byte[]> GetCustomTemplateAsync(Guid templateId)
        {
            try
            {
                // Look for template file with MHTML variants only (.mht, .mhtml)
                var candidates = new[] {
                    Path.Combine(_emailTemplatesPath, $"{templateId}_template.mht"),
                    Path.Combine(_emailTemplatesPath, $"{templateId}_template.mhtml")
                };

                string? filePath = candidates.FirstOrDefault(File.Exists);

                if (filePath == null)
                {
                    throw new FileNotFoundException($"Custom template file not found for template {templateId}");
                }

                return await File.ReadAllBytesAsync(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading custom template: {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<(byte[] FileBytes, string FileName, string ContentType)> GetCustomAttachmentAsync(Guid templateId, int attachmentIndex)
        {
            try
            {
                // Find attachment file matching pattern: {templateId}_{attachmentIndex}_*
                var searchPattern = $"{templateId}_{attachmentIndex}_*";
                var files = Directory.GetFiles(_emailAttachmentsPath, searchPattern);

                if (files.Length == 0)
                {
                    throw new FileNotFoundException($"Custom attachment file not found for template {templateId}, index {attachmentIndex}");
                }

                var filePath = files[0];
                var fileBytes = await File.ReadAllBytesAsync(filePath);
                var fileName = Path.GetFileName(filePath);
                var contentType = GetMimeType(fileName);

                return (fileBytes, fileName, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading custom attachment: TemplateId={TemplateId}, Index={Index}", templateId, attachmentIndex);
                throw;
            }
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
                ".zip" => "application/zip",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }
    }
}
