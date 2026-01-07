namespace XlsxGridFlow.Api.DTOs.Responses;

/// <summary>
/// Response for save operation
/// </summary>
public class SaveResponse
{
    /// <summary>
    /// New version number after save
    /// </summary>
    public required int NewVersion { get; set; }

    /// <summary>
    /// Timestamp of the save operation (ISO 8601)
    /// </summary>
    public required string Timestamp { get; set; }

    /// <summary>
    /// Audit entries for changes introduced in this version
    /// </summary>
    public required List<AuditLogEntryDto> AuditEntries { get; set; }
}
