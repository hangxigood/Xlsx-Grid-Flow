using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using XlsxGridFlow.Functions.Services;

namespace XlsxGridFlow.Functions.Functions;

/// <summary>
/// Function for reverting session to a previous version
/// </summary>
public class RevertSessionFunction
{
    private readonly ILogger<RevertSessionFunction> _logger;
    private readonly BlobSessionService _sessionService;

    public RevertSessionFunction(
        ILogger<RevertSessionFunction> logger,
        BlobSessionService sessionService)
    {
        _logger = logger;
        _sessionService = sessionService;
    }

    [Function("RevertSession")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/revert/{version}")] 
        HttpRequestData req,
        string sessionId,
        int version)
    {
        _logger.LogInformation("Reverting session {SessionId} to version {Version}", sessionId, version);

        try
        {
            var session = await _sessionService.GetSessionAsync(sessionId);
            
            if (session == null)
            {
                return await CreateErrorResponse(req, "Session not found", HttpStatusCode.NotFound);
            }

            if (version < 0 || version > session.Version)
            {
                return await CreateErrorResponse(req, $"Invalid version. Must be between 0 and {session.Version}", HttpStatusCode.BadRequest);
            }

            // Reconstruct data at the target version by filtering changelog
            var targetChanges = session.ChangeLog.Where(c => c.Version <= version).ToList();
            
            // For simplicity, we'll just return a message that this feature is not yet implemented
            // In a full implementation, you'd reconstruct the grid state at that version
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                message = "Revert functionality coming soon",
                sessionId = session.SessionId,
                currentVersion = session.Version,
                targetVersion = version
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reverting session {SessionId} to version {Version}", sessionId, version);
            return await CreateErrorResponse(req, ex.Message, HttpStatusCode.InternalServerError);
        }
    }

    private async Task<HttpResponseData> CreateErrorResponse(
        HttpRequestData req, 
        string message, 
        HttpStatusCode statusCode)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }
}
