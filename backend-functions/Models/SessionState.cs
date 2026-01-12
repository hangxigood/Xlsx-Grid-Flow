using XlsxGridFlow.Functions.DTOs;

namespace XlsxGridFlow.Functions.Models;

/// <summary>
/// Session state stored in Blob Storage
/// </summary>
public class SessionState
{
    /// <summary>
    /// Unique session identifier
    /// </summary>
    public required string SessionId { get; set; }

    /// <summary>
    /// Current version number (increments with each save)
    /// </summary>
    public int Version { get; set; } = 0;

    /// <summary>
    /// Original filename
    /// </summary>
    public required string Filename { get; set; }

    /// <summary>
    /// Column definitions
    /// </summary>
    public required List<ColumnDefDto> ColumnDefs { get; set; }

    /// <summary>
    /// Merged cell ranges
    /// </summary>
    public required List<MergedCellDto> MergedCells { get; set; }

    /// <summary>
    /// Current snapshot of row data
    /// </summary>
    public required List<GridRowDto> CurrentSnapshot { get; set; }

    /// <summary>
    /// Complete change log (all audit entries)
    /// </summary>
    public List<AuditLogEntryDto> ChangeLog { get; set; } = new();

    /// <summary>
    /// Session creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last access timestamp
    /// </summary>
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
}
