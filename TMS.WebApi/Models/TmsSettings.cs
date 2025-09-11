namespace TMS.WebApi.Models
{
    /// <summary>
    /// Configuration settings for the Template Management System
    /// </summary>
    public class TmsSettings
    {
        /// <summary>
        /// How long to retain generated documents (in hours)
        /// </summary>
        public double DocumentRetentionHours { get; set; } = 24.0;

        /// <summary>
        /// How often to run cleanup process (in minutes)
        /// </summary>
        public int CleanupIntervalMinutes { get; set; } = 30;

        /// <summary>
        /// Maximum file size for uploads (in MB)
        /// </summary>
        public int MaxFileSizeMB { get; set; } = 100;

        /// <summary>
        /// Allowed file types for template uploads
        /// </summary>
        public string[] AllowedFileTypes { get; set; } = { ".docx", ".xlsx", ".pptx" };

        /// <summary>
        /// LibreOffice process timeout (in milliseconds)
        /// </summary>
        public int LibreOfficeTimeout { get; set; } = 30000;

        /// <summary>
        /// Shared storage path for generated documents (production-safe)
        /// </summary>
        public string? SharedStoragePath { get; set; }

        /// <summary>
        /// Temporary upload path for file processing
        /// </summary>
        public string? TempUploadPath { get; set; }
    }
}
