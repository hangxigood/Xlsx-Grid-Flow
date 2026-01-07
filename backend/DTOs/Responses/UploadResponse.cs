namespace XlsxGridFlow.Api.DTOs.Responses;

/// <summary>
/// Response for template upload
/// </summary>
public class UploadResponse
{
    /// <summary>
    /// Session identifier (GUID)
    /// </summary>
    public required string SessionId { get; set; }

    /// <summary>
    /// Session expiration timestamp (ISO 8601)
    /// </summary>
    public required string ExpiresAt { get; set; }

    /// <summary>
    /// Parsed template data
    /// </summary>
    public required TemplateDto Template { get; set; }
}
