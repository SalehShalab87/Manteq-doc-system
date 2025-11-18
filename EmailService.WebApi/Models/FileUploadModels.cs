namespace EmailService.WebApi.Models;

/// <summary>
/// Model for custom template file upload
/// </summary>
public class CustomTemplateUploadRequest
{
    /// <summary>
    /// The HTML template file to upload
    /// </summary>
    public IFormFile File { get; set; } = null!;
}

/// <summary>
/// Model for custom attachment file upload
/// </summary>
public class CustomAttachmentUploadRequest
{
    /// <summary>
    /// The attachment file to upload
    /// </summary>
    public IFormFile File { get; set; } = null!;
}
