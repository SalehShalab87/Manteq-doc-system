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
/// Model for uploading an Excel file from a form
/// </summary>
public class ParseExcelRequest
{
    /// <summary>
    /// Excel file (.xlsx) to parse into property values
    /// </summary>
    public IFormFile ExcelFile { get; set; } = null!;
}
