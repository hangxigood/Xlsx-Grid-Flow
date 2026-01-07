namespace XlsxGridFlow.Api.DTOs;

/// <summary>
/// Represents a merged cell range in the Excel template
/// </summary>
public class MergedCellDto
{
    /// <summary>
    /// Starting row (1-based)
    /// </summary>
    public required int StartRow { get; set; }

    /// <summary>
    /// Starting column (1-based)
    /// </summary>
    public required int StartCol { get; set; }

    /// <summary>
    /// Ending row (1-based)
    /// </summary>
    public required int EndRow { get; set; }

    /// <summary>
    /// Ending column (1-based)
    /// </summary>
    public required int EndCol { get; set; }
}
