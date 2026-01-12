using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using XlsxGridFlow.Functions.DTOs;
using XlsxGridFlow.Functions.Services;

namespace XlsxGridFlow.Functions.Functions;

/// <summary>
/// Function for saving session changes
/// </summary>
public class SaveSessionFunction
{
    private readonly ILogger<SaveSessionFunction> _logger;
    private readonly BlobSessionService _sessionService;
    private readonly DiffService _diffService;
    private readonly FormulaService _formulaService;

    public SaveSessionFunction(
        ILogger<SaveSessionFunction> logger,
        BlobSessionService sessionService,
        DiffService diffService,
        FormulaService formulaService)
    {
        _logger = logger;
        _sessionService = sessionService;
        _diffService = diffService;
        _formulaService = formulaService;
    }

    [Function("SaveSession")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/{sessionId}/save")] 
        HttpRequestData req,
        string sessionId)
    {
        _logger.LogInformation("Saving session: {SessionId}", sessionId);

        try
        {
            // Get current session
            var session = await _sessionService.GetSessionAsync(sessionId);
            if (session == null)
            {
                return await CreateErrorResponse(req, "Session not found", HttpStatusCode.NotFound);
            }

            // Parse new data from request
            var saveRequest = await req.ReadFromJsonAsync<SaveRequest>();
            if (saveRequest?.RowData == null)
            {
                return await CreateErrorResponse(req, "Invalid request - rowData is required", HttpStatusCode.BadRequest);
            }

            // Recalculate formulas server-side
            var recalculatedData = _formulaService.RecalculateFormulas(
                session.ColumnDefs,
                saveRequest.RowData
            );

            // Calculate diff
            var newVersion = session.Version + 1;
            var changes = _diffService.CalculateDiff(
                session.CurrentSnapshot,
                recalculatedData,
                session.ColumnDefs,
                newVersion
            );

            // Update session
            session.Version = newVersion;
            session.CurrentSnapshot = recalculatedData;
            session.ChangeLog.AddRange(changes);

            await _sessionService.UpdateSessionAsync(sessionId, session);

            // Return response matching frontend SaveResponse interface
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                newVersion = session.Version,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                auditEntries = changes.Select(c => new
                {
                    version = c.Version,
                    timestamp = c.Timestamp,
                    cellReference = c.CellReference,
                    oldValue = c.OldValue,
                    newValue = c.NewValue
                }).ToList()
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving session {SessionId}", sessionId);
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

public record SaveRequest(List<GridRowDto> RowData);
