namespace XlsxGridFlow.Api.DTOs.Responses;

/// <summary>
/// Response containing full audit history
/// </summary>
public class AuditHistoryResponse
{
    /// <summary>
    /// Session identifier
    /// </summary>
    public required string SessionId { get; set; }

    /// <summary>
    /// History grouped by version
    /// </summary>
    public required List<VersionHistory> History { get; set; }
}

/// <summary>
/// Represents changes for a specific version
/// </summary>
public class VersionHistory
{
    /// <summary>
    /// Version number
    /// </summary>
    public required int Version { get; set; }

    /// <summary>
    /// Timestamp of this version (ISO 8601)
    /// </summary>
    public required string Timestamp { get; set; }

    /// <summary>
    /// Audit entries for this version
    /// </summary>
    public required List<AuditLogEntryDto> Entries { get; set; }
}
