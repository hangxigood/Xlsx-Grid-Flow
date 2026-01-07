namespace XlsxGridFlow.Api.DTOs.Responses;

/// <summary>
/// Standardized error response
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error code identifier
    /// </summary>
    public required string ErrorCode { get; set; }

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Optional detailed validation errors
    /// </summary>
    public Dictionary<string, string>? Details { get; set; }
}
