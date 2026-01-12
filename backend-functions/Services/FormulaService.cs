using OfficeOpenXml;
using Microsoft.Extensions.Logging;
using XlsxGridFlow.Functions.DTOs;
using XlsxGridFlow.Functions.Models;
using XlsxGridFlow.Functions.Utilities;

namespace XlsxGridFlow.Functions.Services;

/// <summary>
/// Service for calculating formulas using EPPlus
/// </summary>
public class FormulaService
{
    private readonly ILogger<FormulaService> _logger;

    public FormulaService(ILogger<FormulaService> logger)
    {
        _logger = logger;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <summary>
    /// Recalculates all formulas in the session and returns updated row data
    /// </summary>
    public List<GridRowDto> RecalculateFormulas(
        List<ColumnDefDto> columnDefs,
        List<GridRowDto> rowData)
    {
        _logger.LogInformation("RecalculateFormulas called with {RowCount} rows", rowData.Count);
        
        try
        {
            // Create an in-memory Excel worksheet for calculation
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Data");

            // Step 1: Write headers (Row 1)
            for (int i = 0; i < columnDefs.Count; i++)
            {
                worksheet.Cells[1, i + 1].Value = columnDefs[i].HeaderName;
            }

            // Step 2: Write data and formulas to worksheet
            foreach (var row in rowData)
            {
                foreach (var colDef in columnDefs)
                {
                    var cellValue = row.Cells.GetValueOrDefault(colDef.Field);
                    var col = GetColumnIndex(colDef.Field);
                    var excelRow = row.RowId;
                    var cell = worksheet.Cells[excelRow, col];

                    // Extract actual value from JsonElement if needed
                    var actualValue = JsonHelper.ExtractValue(cellValue);
                    
                    if (actualValue is string strValue && strValue.StartsWith("="))
                    {
                        // It's a formula - set the formula
                        var formula = strValue.Substring(1); // Remove the = prefix
                        cell.Formula = formula;
                    }
                    else
                    {
                        // Regular value
                        cell.Value = ConvertToExcelValue(actualValue, colDef.DataType);
                    }
                }
            }

            // Step 3: Calculate all formulas
            worksheet.Calculate();

            // Step 4: Read back the calculated values
            var result = new List<GridRowDto>();
            foreach (var row in rowData)
            {
                var newCells = new Dictionary<string, object?>();

                foreach (var colDef in columnDefs)
                {
                    var col = GetColumnIndex(colDef.Field);
                    var excelRow = row.RowId;
                    var cell = worksheet.Cells[excelRow, col];

                    object? value;

                    if (!string.IsNullOrEmpty(cell.Formula))
                    {
                        // For formula cells, store the CALCULATED RESULT, not the formula string
                        value = ConvertFromExcelValue(cell.Value, colDef.DataType);
                    }
                    else
                    {
                        // Get the value and convert it
                        value = ConvertFromExcelValue(cell.Value, colDef.DataType);
                    }

                    newCells[colDef.Field] = value;
                }

                result.Add(new GridRowDto
                {
                    RowId = row.RowId,
                    Cells = newCells
                });
            }

            _logger.LogDebug("Recalculated formulas for {RowCount} rows", rowData.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recalculate formulas");
            // Return original data if calculation fails
            return rowData;
        }
    }

    /// <summary>
    /// Converts a value to Excel-compatible format
    /// </summary>
    private object? ConvertToExcelValue(object? value, DataType dataType)
    {
        if (value == null)
        {
            return null;
        }

        return dataType switch
        {
            DataType.Date when value is string dateStr => 
                DateTime.TryParse(dateStr, out var dt) ? dt : value,
            DataType.Number when value is string numStr => 
                double.TryParse(numStr, out var d) ? d : value,
            DataType.Boolean when value is string boolStr => 
                bool.TryParse(boolStr, out var b) ? b : value,
            _ => value
        };
    }

    /// <summary>
    /// Converts Excel value back to API format
    /// </summary>
    private object? ConvertFromExcelValue(object? value, DataType dataType)
    {
        if (value == null)
        {
            return null;
        }

        return dataType switch
        {
            DataType.Date when value is DateTime dt => dt.ToString("yyyy-MM-dd"),
            DataType.Date when value is double d => DateTime.FromOADate(d).ToString("yyyy-MM-dd"),
            DataType.Number => value,
            DataType.Boolean => value,
            DataType.Text => value?.ToString(),
            _ => value
        };
    }

    /// <summary>
    /// Converts Excel column letter to index (A -> 1, B -> 2, etc.)
    /// </summary>
    private int GetColumnIndex(string columnLetter)
    {
        int index = 0;
        for (int i = 0; i < columnLetter.Length; i++)
        {
            index *= 26;
            index += (columnLetter[i] - 'A' + 1);
        }
        return index;
    }
}
