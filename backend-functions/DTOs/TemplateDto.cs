namespace XlsxGridFlow.Functions.DTOs;

/// <summary>
/// Complete template structure parsed from Excel file
/// </summary>
public class TemplateDto
{
    /// <summary>
    /// Original Excel filename
    /// </summary>
    public required string Filename { get; set; }

    /// <summary>
    /// Column definitions for AG-Grid
    /// </summary>
    public required List<ColumnDefDto> ColumnDefs { get; set; }

    /// <summary>
    /// Grid row data
    /// </summary>
    public required List<GridRowDto> RowData { get; set; }

    /// <summary>
    /// Merged cell ranges
    /// </summary>
    public required List<MergedCellDto> MergedCells { get; set; }
}
