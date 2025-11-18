namespace TMS.WebApi.Models;

/// <summary>
/// Model for template file upload
/// </summary>
public class TemplateFileUploadRequest
{
    /// <summary>
    /// The template file to upload (.docx or .xlsx)
    /// </summary>
    public IFormFile TemplateFile { get; set; } = null!;
}

/// <summary>
/// Model for test generation with Excel file
/// </summary>
public class TestGenerationRequest
{
    /// <summary>
    /// Excel file containing property values for test generation
    /// </summary>
    public IFormFile ExcelFile { get; set; } = null!;
}
