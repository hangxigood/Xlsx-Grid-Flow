using XlsxGridFlow.Api.Models;

namespace XlsxGridFlow.Api.DTOs;

/// <summary>
/// Column definition for AG-Grid configuration
/// </summary>
public class ColumnDefDto
{
    /// <summary>
    /// Excel column reference (e.g., "A", "B", "C")
    /// </summary>
    public required string Field { get; set; }

    /// <summary>
    /// Display name for the column header (without tags)
    /// </summary>
    public required string HeaderName { get; set; }

    /// <summary>
    /// Data type for validation and formatting
    /// </summary>
    public required DataType DataType { get; set; }

    /// <summary>
    /// Whether the column is editable
    /// </summary>
    public required bool Editable { get; set; }
}
