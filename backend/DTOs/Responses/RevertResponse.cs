namespace XlsxGridFlow.Api.DTOs.Responses;

/// <summary>
/// Response for revert operation
/// </summary>
public class RevertResponse
{
    /// <summary>
    /// New version number after revert
    /// </summary>
    public required int NewVersion { get; set; }

    /// <summary>
    /// Restored row data
    /// </summary>
    public required List<GridRowDto> RowData { get; set; }

    /// <summary>
    /// Audit entries representing the revert action
    /// </summary>
    public required List<AuditLogEntryDto> AuditEntries { get; set; }
}
