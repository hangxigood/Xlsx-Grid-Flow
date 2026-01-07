namespace XlsxGridFlow.Api.Configuration;

/// <summary>
/// CORS configuration
/// </summary>
public class CorsSettings
{
    /// <summary>
    /// Allowed origins for CORS
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
