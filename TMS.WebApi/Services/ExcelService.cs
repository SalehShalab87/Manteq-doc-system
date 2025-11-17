using OfficeOpenXml;
using System.Text.Json;

namespace TMS.WebApi.Services
{
    /// <summary>
    /// Service for generating and reading Excel files for template placeholder testing
    /// </summary>
    public interface IExcelService
    {
        Task<byte[]> GeneratePlaceholdersExcelAsync(List<string> placeholders);
        Task<Dictionary<string, string>> ReadExcelToJsonAsync(Stream excelStream);
    }

    /// <summary>
    /// Implementation of Excel service using EPPlus
    /// </summary>
    public class ExcelService : IExcelService
    {
        private readonly ILogger<ExcelService> _logger;

        public ExcelService(ILogger<ExcelService> logger)
        {
            _logger = logger;
            // Set EPPlus license context (NonCommercial for free use)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// Generate an Excel file with two columns: Placeholder | Value
        /// Placeholders are pre-filled, values are empty for user to fill
        /// </summary>
        public async Task<byte[]> GeneratePlaceholdersExcelAsync(List<string> placeholders)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var package = new ExcelPackage();
                    var worksheet = package.Workbook.Worksheets.Add("Placeholders");

                    // Set column headers with styling
                    worksheet.Cells[1, 1].Value = "Placeholder";
                    worksheet.Cells[1, 2].Value = "Value";

                    // Style headers
                    using (var headerRange = worksheet.Cells[1, 1, 1, 2])
                    {
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                        headerRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        headerRange.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thick;
                    }

                    // Add placeholders to column A, starting from row 2
                    for (int i = 0; i < placeholders.Count; i++)
                    {
                        int row = i + 2;
                        worksheet.Cells[row, 1].Value = placeholders[i];
                        
                        // Style placeholder cells (read-only appearance)
                        worksheet.Cells[row, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                        worksheet.Cells[row, 1].Style.Font.Bold = true;
                        
                        // Style value cells (editable appearance)
                        worksheet.Cells[row, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[row, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.White);
                    }

                    // Auto-fit columns
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                    
                    // Set minimum column widths
                    worksheet.Column(1).Width = Math.Max(worksheet.Column(1).Width, 30);
                    worksheet.Column(2).Width = Math.Max(worksheet.Column(2).Width, 40);

                    // Freeze header row
                    worksheet.View.FreezePanes(2, 1);

                    // Add instructions in a separate sheet
                    var instructionsSheet = package.Workbook.Worksheets.Add("Instructions");
                    instructionsSheet.Cells[1, 1].Value = "HOW TO USE THIS EXCEL FILE";
                    instructionsSheet.Cells[1, 1].Style.Font.Bold = true;
                    instructionsSheet.Cells[1, 1].Style.Font.Size = 14;
                    
                    instructionsSheet.Cells[3, 1].Value = "1. Go to the 'Placeholders' sheet";
                    instructionsSheet.Cells[4, 1].Value = "2. Fill in the 'Value' column (Column B) for each placeholder";
                    instructionsSheet.Cells[5, 1].Value = "3. Do NOT modify the 'Placeholder' column (Column A)";
                    instructionsSheet.Cells[6, 1].Value = "4. Save the file";
                    instructionsSheet.Cells[7, 1].Value = "5. Upload it back to test your template";
                    
                    instructionsSheet.Cells[9, 1].Value = "Note: Empty values will be treated as empty strings in the generated document.";
                    instructionsSheet.Cells[9, 1].Style.Font.Italic = true;
                    
                    instructionsSheet.Column(1).Width = 80;

                    _logger.LogInformation("Generated Excel file with {PlaceholderCount} placeholders", placeholders.Count);

                    return package.GetAsByteArray();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating placeholders Excel file");
                    throw;
                }
            });
        }

        /// <summary>
        /// Read an Excel file and convert it to a dictionary of placeholder-value pairs
        /// Expects two columns: Placeholder | Value
        /// </summary>
        public async Task<Dictionary<string, string>> ReadExcelToJsonAsync(Stream excelStream)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var propertyValues = new Dictionary<string, string>();

                    using var package = new ExcelPackage(excelStream);
                    var worksheet = package.Workbook.Worksheets["Placeholders"];

                    if (worksheet == null)
                    {
                        throw new ArgumentException("Excel file must contain a 'Placeholders' sheet");
                    }

                    // Start from row 2 (skip header)
                    var rowCount = worksheet.Dimension?.Rows ?? 0;
                    
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var placeholder = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                        var value = worksheet.Cells[row, 2].Value?.ToString() ?? string.Empty;

                        if (!string.IsNullOrWhiteSpace(placeholder))
                        {
                            propertyValues[placeholder] = value;
                        }
                    }

                    _logger.LogInformation("Read {PropertyCount} property values from Excel file", propertyValues.Count);

                    return propertyValues;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading Excel file");
                    throw new ArgumentException("Failed to read Excel file. Ensure it's a valid Excel file with 'Placeholders' sheet.", ex);
                }
            });
        }
    }
}
