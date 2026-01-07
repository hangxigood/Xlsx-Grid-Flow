using OfficeOpenXml;
using System.Text.RegularExpressions;
using XlsxGridFlow.Api.DTOs;
using XlsxGridFlow.Api.Exceptions;
using XlsxGridFlow.Api.Models;

namespace XlsxGridFlow.Api.Services;

/// <summary>
/// Service for parsing Excel files using EPPlus
/// </summary>
public class ExcelService
{
    private readonly ILogger<ExcelService> _logger;

    public ExcelService(ILogger<ExcelService> logger)
    {
        _logger = logger;
        // Set EPPlus license context for non-commercial use
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <summary>
    /// Parses an Excel file and extracts template data
    /// </summary>
    public TemplateDto ParseExcelFile(Stream fileStream, string filename)
    {
        try
        {
            using var package = new ExcelPackage(fileStream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault()
                ?? throw new ParsingException("No worksheets found in the Excel file");

            // Validate that data starts at A1
            if (worksheet.Dimension == null)
            {
                throw new ParsingException("Worksheet is empty");
            }

            var columnDefs = ParseHeaderConventions(worksheet);
            var mergedCells = ExtractMergedCells(worksheet);
            var rowData = ExtractRowData(worksheet, columnDefs);

            return new TemplateDto
            {
                Filename = filename,
                ColumnDefs = columnDefs,
                RowData = rowData,
                MergedCells = mergedCells
            };
        }
        catch (Exception ex) when (ex is not AppException)
        {
            _logger.LogError(ex, "Failed to parse Excel file: {Filename}", filename);
            throw new ParsingException($"Failed to parse Excel file: {ex.Message}");
        }
    }

    /// <summary>
    /// Parses Row 1 headers and extracts column conventions
    /// </summary>
    private List<ColumnDefDto> ParseHeaderConventions(ExcelWorksheet worksheet)
    {
        var columnDefs = new List<ColumnDefDto>();
        var dimension = worksheet.Dimension;
        var headerNames = new HashSet<string>();

        for (int col = 1; col <= dimension.End.Column; col++)
        {
            var headerCell = worksheet.Cells[1, col];
            var headerValue = headerCell.Text?.Trim();

            // Skip empty headers
            if (string.IsNullOrWhiteSpace(headerValue))
            {
                continue;
            }

            var (cleanName, dataType, editable) = ParseHeaderString(headerValue);

            // Handle duplicate headers
            var field = GetColumnLetter(col);
            var uniqueHeaderName = cleanName;
            int suffix = 1;
            while (headerNames.Contains(uniqueHeaderName))
            {
                uniqueHeaderName = $"{cleanName}_{suffix}";
                suffix++;
            }
            headerNames.Add(uniqueHeaderName);

            // If type not specified, infer from first data row
            if (dataType == null)
            {
                dataType = InferDataType(worksheet, col);
            }

            columnDefs.Add(new ColumnDefDto
            {
                Field = field,
                HeaderName = uniqueHeaderName,
                DataType = dataType.Value,
                Editable = editable
            });
        }

        return columnDefs;
    }

    /// <summary>
    /// Parses header string to extract clean name, data type, and editability
    /// </summary>
    private (string cleanName, DataType? dataType, bool editable) ParseHeaderString(string header)
    {
        // Pattern: "Name (text)" or "Total (ReadOnly)"
        var match = Regex.Match(header, @"^(.+?)\s*\((.+?)\)\s*$");

        if (!match.Success)
        {
            // No tag found, default to editable with auto-detection
            return (header, null, true);
        }

        var cleanName = match.Groups[1].Value.Trim();
        var tag = match.Groups[2].Value.Trim().ToLowerInvariant();

        // Check for ReadOnly
        if (tag == "readonly")
        {
            return (cleanName, DataType.Text, false);
        }

        // Parse data type
        var dataType = tag switch
        {
            "text" => DataType.Text,
            "number" => DataType.Number,
            "date" => DataType.Date,
            "boolean" => DataType.Boolean,
            "formula" => DataType.Formula,
            _ => (DataType?)null
        };

        // Formula columns should be read-only (calculated values)
        bool editable = dataType != DataType.Formula;

        return (cleanName, dataType, editable);
    }

    /// <summary>
    /// Infers data type from the first data row (Row 2)
    /// </summary>
    private DataType InferDataType(ExcelWorksheet worksheet, int col)
    {
        if (worksheet.Dimension.End.Row < 2)
        {
            return DataType.Text; // No data rows, default to text
        }

        var cell = worksheet.Cells[2, col];
        
        // Check if it's a formula
        if (!string.IsNullOrEmpty(cell.Formula))
        {
            return DataType.Formula;
        }

        var value = cell.Value;

        if (value == null)
        {
            return DataType.Text;
        }

        // Type inference based on EPPlus value type
        return value switch
        {
            double or int or long or decimal or float => DataType.Number,
            DateTime => DataType.Date,
            bool => DataType.Boolean,
            _ => DataType.Text
        };
    }

    /// <summary>
    /// Extracts merged cell ranges from the worksheet
    /// </summary>
    private List<MergedCellDto> ExtractMergedCells(ExcelWorksheet worksheet)
    {
        var mergedCells = new List<MergedCellDto>();

        foreach (var mergedRange in worksheet.MergedCells)
        {
            var range = worksheet.Cells[mergedRange];
            mergedCells.Add(new MergedCellDto
            {
                StartRow = range.Start.Row,
                StartCol = range.Start.Column,
                EndRow = range.End.Row,
                EndCol = range.End.Column
            });
        }

        return mergedCells;
    }

    /// <summary>
    /// Extracts row data from the worksheet
    /// </summary>
    private List<GridRowDto> ExtractRowData(ExcelWorksheet worksheet, List<ColumnDefDto> columnDefs)
    {
        var rowData = new List<GridRowDto>();
        var dimension = worksheet.Dimension;

        // Start from row 2 (row 1 is headers)
        for (int row = 2; row <= dimension.End.Row; row++)
        {
            var cells = new Dictionary<string, object?>();

            foreach (var colDef in columnDefs)
            {
                var col = GetColumnIndex(colDef.Field);
                var cell = worksheet.Cells[row, col];

                object? cellValue;

                // Check if cell has a formula
                if (!string.IsNullOrEmpty(cell.Formula))
                {
                    // Store formula string with = prefix
                    cellValue = $"={cell.Formula}";
                }
                else
                {
                    cellValue = ConvertCellValue(cell.Value, colDef.DataType);
                }

                cells[colDef.Field] = cellValue;
            }

            rowData.Add(new GridRowDto
            {
                RowId = row,
                Cells = cells
            });
        }

        return rowData;
    }

    /// <summary>
    /// Converts cell value to appropriate type
    /// </summary>
    private object? ConvertCellValue(object? value, DataType dataType)
    {
        if (value == null)
        {
            return null;
        }

        return dataType switch
        {
            DataType.Number => value switch
            {
                double d => d,
                int i => i,
                long l => l,
                decimal dec => dec,
                float f => f,
                string s when double.TryParse(s, out var d) => d,
                _ => value
            },
            DataType.Date => value switch
            {
                DateTime dt => dt.ToString("yyyy-MM-dd"),
                double d => DateTime.FromOADate(d).ToString("yyyy-MM-dd"),
                _ => value?.ToString()
            },
            DataType.Boolean => value switch
            {
                bool b => b,
                string s when bool.TryParse(s, out var b) => b,
                _ => value
            },
            DataType.Text => value?.ToString(),
            DataType.Formula => value?.ToString(),
            _ => value
        };
    }

    /// <summary>
    /// Converts column index to Excel column letter (1 -> A, 2 -> B, etc.)
    /// </summary>
    private string GetColumnLetter(int columnIndex)
    {
        string columnLetter = "";
        while (columnIndex > 0)
        {
            int modulo = (columnIndex - 1) % 26;
            columnLetter = Convert.ToChar('A' + modulo) + columnLetter;
            columnIndex = (columnIndex - modulo) / 26;
        }
        return columnLetter;
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
