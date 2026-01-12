using XlsxGridFlow.Api.DTOs;
using XlsxGridFlow.Api.Models;

namespace XlsxGridFlow.Api.Utilities;

/// <summary>
/// Centralized coordinate mapping utility for converting between:
/// - Excel coordinates (1-based rows, letter columns)
/// - Grid coordinates (rowId-based with field names)
/// - EPPlus coordinates (1-based rows and columns)
/// </summary>
public static class CoordinateMapper
{
    /// <summary>
    /// Excel coordinate (e.g., "B4" means Column B, Row 4)
    /// </summary>
    public record ExcelCoordinate(int Row, string Column);

    /// <summary>
    /// Grid coordinate using rowId and field name
    /// </summary>
    public record GridCoordinate(int RowId, string Field);

    /// <summary>
    /// EPPlus coordinate (1-based row and column indices)
    /// </summary>
    public record EPPlusCoordinate(int Row, int Column);

    // ============================================================================
    // Column Letter ↔ Index Conversions
    // ============================================================================

    /// <summary>
    /// Converts Excel column letter to 1-based column index
    /// </summary>
    /// <param name="letter">Column letter (A, B, C, ..., Z, AA, AB, ...)</param>
    /// <returns>1-based column index (A=1, B=2, ..., Z=26, AA=27, ...)</returns>
    /// <example>ColumnLetterToIndex("A") → 1</example>
    /// <example>ColumnLetterToIndex("B") → 2</example>
    /// <example>ColumnLetterToIndex("AA") → 27</example>
    public static int ColumnLetterToIndex(string letter)
    {
        int index = 0;
        for (int i = 0; i < letter.Length; i++)
        {
            index *= 26;
            index += (letter[i] - 'A' + 1);
        }
        return index;
    }

    /// <summary>
    /// Converts 1-based column index to Excel letter
    /// </summary>
    /// <param name="index">1-based column index</param>
    /// <returns>Column letter</returns>
    /// <example>IndexToColumnLetter(1) → "A"</example>
    /// <example>IndexToColumnLetter(2) → "B"</example>
    /// <example>IndexToColumnLetter(27) → "AA"</example>
    public static string IndexToColumnLetter(int index)
    {
        string letter = "";
        while (index > 0)
        {
            int modulo = (index - 1) % 26;
            letter = (char)('A' + modulo) + letter;
            index = (index - modulo) / 26;
        }
        return letter;
    }

    // ============================================================================
    // Cell Reference String Parsing
    // ============================================================================

    /// <summary>
    /// Parses Excel cell reference string (e.g., "B4") to coordinates
    /// </summary>
    /// <param name="cellRef">Cell reference string (e.g., "B4", "AA10")</param>
    /// <returns>Excel coordinate or null if invalid</returns>
    /// <example>ParseCellReference("B4") → ExcelCoordinate(4, "B")</example>
    public static ExcelCoordinate? ParseCellReference(string cellRef)
    {
        var match = System.Text.RegularExpressions.Regex.Match(cellRef, @"^([A-Z]+)(\d+)$");
        
        if (!match.Success)
        {
            return null;
        }

        return new ExcelCoordinate(
            Row: int.Parse(match.Groups[2].Value),
            Column: match.Groups[1].Value
        );
    }

    /// <summary>
    /// Formats coordinates as Excel cell reference string
    /// </summary>
    /// <param name="coord">Excel coordinate</param>
    /// <returns>Cell reference string (e.g., "B4")</returns>
    /// <example>FormatCellReference(new ExcelCoordinate(4, "B")) → "B4"</example>
    public static string FormatCellReference(ExcelCoordinate coord)
    {
        return $"{coord.Column}{coord.Row}";
    }

    /// <summary>
    /// Formats grid coordinate as Excel cell reference string
    /// </summary>
    /// <param name="coord">Grid coordinate</param>
    /// <returns>Cell reference string (e.g., "B4")</returns>
    /// <example>FormatCellReference(new GridCoordinate(4, "B")) → "B4"</example>
    public static string FormatCellReference(GridCoordinate coord)
    {
        return $"{coord.Field}{coord.RowId}";
    }

    // ============================================================================
    // Excel ↔ Grid Conversions
    // ============================================================================

    /// <summary>
    /// Converts Excel coordinates to Grid coordinates
    /// </summary>
    /// <example>ExcelToGrid(new ExcelCoordinate(4, "B")) → GridCoordinate(4, "B")</example>
    public static GridCoordinate ExcelToGrid(ExcelCoordinate excel)
    {
        return new GridCoordinate(excel.Row, excel.Column);
    }

    /// <summary>
    /// Converts Grid coordinates to Excel coordinates
    /// </summary>
    /// <example>GridToExcel(new GridCoordinate(4, "B")) → ExcelCoordinate(4, "B")</example>
    public static ExcelCoordinate GridToExcel(GridCoordinate grid)
    {
        return new ExcelCoordinate(grid.RowId, grid.Field);
    }

    // ============================================================================
    // Excel ↔ EPPlus Conversions
    // ============================================================================

    /// <summary>
    /// Converts Excel coordinates to EPPlus coordinates
    /// </summary>
    /// <example>ExcelToEPPlus(new ExcelCoordinate(4, "B")) → EPPlusCoordinate(4, 2)</example>
    public static EPPlusCoordinate ExcelToEPPlus(ExcelCoordinate excel)
    {
        return new EPPlusCoordinate(
            Row: excel.Row,
            Column: ColumnLetterToIndex(excel.Column)
        );
    }

    /// <summary>
    /// Converts EPPlus coordinates to Excel coordinates
    /// </summary>
    /// <example>EPPlusToExcel(new EPPlusCoordinate(4, 2)) → ExcelCoordinate(4, "B")</example>
    public static ExcelCoordinate EPPlusToExcel(EPPlusCoordinate epplus)
    {
        return new ExcelCoordinate(
            Row: epplus.Row,
            Column: IndexToColumnLetter(epplus.Column)
        );
    }

    // ============================================================================
    // Grid ↔ EPPlus Conversions
    // ============================================================================

    /// <summary>
    /// Converts Grid coordinates to EPPlus coordinates
    /// </summary>
    /// <example>GridToEPPlus(new GridCoordinate(4, "B")) → EPPlusCoordinate(4, 2)</example>
    public static EPPlusCoordinate GridToEPPlus(GridCoordinate grid)
    {
        return new EPPlusCoordinate(
            Row: grid.RowId,
            Column: ColumnLetterToIndex(grid.Field)
        );
    }

    /// <summary>
    /// Converts EPPlus coordinates to Grid coordinates
    /// </summary>
    /// <example>EPPlusToGrid(new EPPlusCoordinate(4, 2)) → GridCoordinate(4, "B")</example>
    public static GridCoordinate EPPlusToGrid(EPPlusCoordinate epplus)
    {
        return new GridCoordinate(
            RowId: epplus.Row,
            Field: IndexToColumnLetter(epplus.Column)
        );
    }

    // ============================================================================
    // Validation Helpers
    // ============================================================================

    /// <summary>
    /// Validates that a rowId exists in the row data
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if rowId not found</exception>
    public static void ValidateRowId(int rowId, List<GridRowDto> rowData)
    {
        if (!rowData.Any(row => row.RowId == rowId))
        {
            throw new ArgumentException($"Invalid rowId {rowId} - not found in row data");
        }
    }

    /// <summary>
    /// Validates that a field exists in column definitions
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if field not found</exception>
    public static void ValidateField(string field, List<ColumnDefDto> columnDefs)
    {
        if (!columnDefs.Any(col => col.Field == field))
        {
            throw new ArgumentException($"Invalid field {field} - not found in column definitions");
        }
    }

    /// <summary>
    /// Validates a cell reference string format
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if format is invalid</exception>
    public static void ValidateCellReference(string cellRef)
    {
        if (ParseCellReference(cellRef) == null)
        {
            throw new ArgumentException($"Invalid cell reference format: {cellRef}");
        }
    }
}
