using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using XlsxGridFlow.Functions.Services;

namespace XlsxGridFlow.Functions.Functions;

/// <summary>
/// Function for exporting session as PDF
/// </summary>
public class ExportPdfFunction
{
    private readonly ILogger<ExportPdfFunction> _logger;
    private readonly BlobSessionService _sessionService;
    private readonly PdfService _pdfService;

    public ExportPdfFunction(
        ILogger<ExportPdfFunction> logger,
        BlobSessionService sessionService,
        PdfService pdfService)
    {
        _logger = logger;
        _sessionService = sessionService;
        _pdfService = pdfService;
    }

    [Function("ExportPdf")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session/{sessionId}/export/pdf")] 
        HttpRequestData req,
        string sessionId)
    {
        _logger.LogInformation("Exporting PDF for session: {SessionId}", sessionId);

        try
        {
            var session = await _sessionService.GetSessionAsync(sessionId);
            if (session == null)
            {
                return await CreateErrorResponse(req, "Session not found", HttpStatusCode.NotFound);
            }

            // Generate PDF
            var pdfBytes = _pdfService.GenerateReport(session);

            // Return PDF
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/pdf");
            response.Headers.Add("Content-Disposition", 
                $"attachment; filename=\"{session.Filename}.pdf\"");
            await response.Body.WriteAsync(pdfBytes);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting PDF for session {SessionId}", sessionId);
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
