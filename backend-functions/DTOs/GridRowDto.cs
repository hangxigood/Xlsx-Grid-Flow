namespace XlsxGridFlow.Functions.DTOs;

/// <summary>
/// Represents a single row in the grid with dynamic columns
/// </summary>
public class GridRowDto
{
    /// <summary>
    /// 1-based index matching the original Excel row number
    /// </summary>
    public required int RowId { get; set; }

    /// <summary>
    /// Dynamic cell values keyed by column field (e.g., "A", "B", "C")
    /// Values can be string, number, boolean, or null
    /// </summary>
    public required Dictionary<string, object?> Cells { get; set; }
}
