namespace XlsxGridFlow.Functions.DTOs;

/// <summary>
/// Represents a single change in the audit trail
/// </summary>
public class AuditLogEntryDto
{
    /// <summary>
    /// Version number this change belongs to
    /// </summary>
    public required int Version { get; set; }

    /// <summary>
    /// Timestamp of the change (ISO 8601)
    /// </summary>
    public required string Timestamp { get; set; }

    /// <summary>
    /// Cell reference (e.g., "B4")
    /// </summary>
    public required string CellReference { get; set; }

    /// <summary>
    /// Previous value
    /// </summary>
    public object? OldValue { get; set; }

    /// <summary>
    /// New value
    /// </summary>
    public object? NewValue { get; set; }
}
