using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using XlsxGridFlow.Functions.DTOs;
using XlsxGridFlow.Functions.Services;

namespace XlsxGridFlow.Functions.Functions;

/// <summary>
/// Function for getting session data
/// </summary>
public class GetSessionFunction
{
    private readonly ILogger<GetSessionFunction> _logger;
    private readonly BlobSessionService _sessionService;

    public GetSessionFunction(
        ILogger<GetSessionFunction> logger,
        BlobSessionService sessionService)
    {
        _logger = logger;
        _sessionService = sessionService;
    }

    [Function("GetSession")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session/{sessionId}")] 
        HttpRequestData req,
        string sessionId)
    {
        _logger.LogInformation("Getting session: {SessionId}", sessionId);

        try
        {
            var session = await _sessionService.GetSessionAsync(sessionId);
            
            if (session == null)
            {
                return await CreateErrorResponse(req, "Session not found", HttpStatusCode.NotFound);
            }

            // Return response
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                sessionId = session.SessionId,
                version = session.Version,
                filename = session.Filename,
                columnDefs = session.ColumnDefs,
                rowData = session.CurrentSnapshot,
                mergedCells = session.MergedCells,
                changeLog = session.ChangeLog
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session {SessionId}", sessionId);
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
