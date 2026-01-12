using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using XlsxGridFlow.Functions.Services;

namespace XlsxGridFlow.Functions.Functions;

/// <summary>
/// Function for getting audit history
/// </summary>
public class GetAuditHistoryFunction
{
    private readonly ILogger<GetAuditHistoryFunction> _logger;
    private readonly BlobSessionService _sessionService;

    public GetAuditHistoryFunction(
        ILogger<GetAuditHistoryFunction> logger,
        BlobSessionService sessionService)
    {
        _logger = logger;
        _sessionService = sessionService;
    }

    [Function("GetAuditHistory")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session/{sessionId}/audit")] 
        HttpRequestData req,
        string sessionId)
    {
        _logger.LogInformation("Getting audit history for session: {SessionId}", sessionId);

        try
        {
            var session = await _sessionService.GetSessionAsync(sessionId);
            
            if (session == null)
            {
                return await CreateErrorResponse(req, "Session not found", HttpStatusCode.NotFound);
            }

            // Group changeLog by version (exclude version 0 - initial state)
            var history = session.ChangeLog
                .Where(entry => entry.Version > 1)
                .GroupBy(entry => entry.Version)
                .Select(group => new
                {
                    version = group.Key,
                    timestamp = group.First().Timestamp,
                    entries = group.Select(e => new
                    {
                        version = e.Version,
                        timestamp = e.Timestamp,
                        cellReference = e.CellReference,
                        oldValue = e.OldValue,
                        newValue = e.NewValue
                    }).ToList()
                })
                .OrderByDescending(g => g.version)
                .ToList();

            // Return response
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                sessionId = session.SessionId,
                history
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit history for session {SessionId}", sessionId);
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
