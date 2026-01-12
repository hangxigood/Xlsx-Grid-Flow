using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using XlsxGridFlow.Functions.Services;

namespace XlsxGridFlow.Functions.Functions;

/// <summary>
/// Timer function for cleaning up expired sessions
/// </summary>
public class CleanupExpiredSessionsFunction
{
    private readonly ILogger<CleanupExpiredSessionsFunction> _logger;
    private readonly BlobSessionService _sessionService;

    public CleanupExpiredSessionsFunction(
        ILogger<CleanupExpiredSessionsFunction> logger,
        BlobSessionService sessionService)
    {
        _logger = logger;
        _sessionService = sessionService;
    }

    [Function("CleanupExpiredSessions")]
    public async Task Run(
        [TimerTrigger("0 */30 * * * *")] TimerInfo timer) // Every 30 minutes
    {
        _logger.LogInformation("Starting session cleanup at {Time}", DateTime.UtcNow);

        try
        {
            var deletedCount = await _sessionService.CleanupExpiredSessionsAsync();
            _logger.LogInformation("Cleaned up {DeletedCount} expired sessions", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during session cleanup");
        }
    }
}
