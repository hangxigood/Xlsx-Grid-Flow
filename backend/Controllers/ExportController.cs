using Microsoft.AspNetCore.Mvc;
using XlsxGridFlow.Api.DTOs.Responses;
using XlsxGridFlow.Api.Services;

namespace XlsxGridFlow.Api.Controllers;

[ApiController]
[Route("api/session")]
public class ExportController : ControllerBase
{
    private readonly SessionService _sessionService;
    private readonly PdfService _pdfService;
    private readonly ILogger<ExportController> _logger;

    public ExportController(
        SessionService sessionService,
        PdfService pdfService,
        ILogger<ExportController> logger)
    {
        _sessionService = sessionService;
        _pdfService = pdfService;
        _logger = logger;
    }

    /// <summary>
    /// Exports a session as a PDF report
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <returns>PDF file</returns>
    [HttpGet("{sessionId}/export/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult ExportPdf(string sessionId)
    {
        _logger.LogInformation("Exporting PDF for session {SessionId}", sessionId);

        var session = _sessionService.GetSession(sessionId);
        var pdfBytes = _pdfService.GenerateReport(session);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var filename = $"report-{timestamp}.pdf";

        return File(pdfBytes, "application/pdf", filename);
    }
}
