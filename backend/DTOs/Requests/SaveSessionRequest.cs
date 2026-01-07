namespace XlsxGridFlow.Api.DTOs.Requests;

/// <summary>
/// Request body for saving session changes
/// </summary>
public class SaveSessionRequest
{
    /// <summary>
    /// Updated row data
    /// </summary>
    public required List<GridRowDto> RowData { get; set; }

    /// <summary>
    /// Current version known by client (for optimistic concurrency control)
    /// </summary>
    public required int ClientVersion { get; set; }
}
