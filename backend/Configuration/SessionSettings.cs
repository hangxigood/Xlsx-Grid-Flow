namespace XlsxGridFlow.Api.Configuration;

/// <summary>
/// Session management configuration
/// </summary>
public class SessionSettings
{
    /// <summary>
    /// Session expiration time in minutes
    /// </summary>
    public int ExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// Maximum file size in megabytes
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 10;
}
