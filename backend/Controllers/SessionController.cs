using Microsoft.AspNetCore.Mvc;
using XlsxGridFlow.Api.DTOs.Requests;
using XlsxGridFlow.Api.DTOs.Responses;
using XlsxGridFlow.Api.Services;

namespace XlsxGridFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionController : ControllerBase
{
    private readonly SessionService _sessionService;
    private readonly ILogger<SessionController> _logger;

    public SessionController(
        SessionService sessionService,
        ILogger<SessionController> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Saves changes to a session and creates a new version
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="request">Save request with row data and client version</param>
    /// <returns>New version information and audit entries</returns>
    [HttpPost("{sessionId}/save")]
    [ProducesResponseType(typeof(SaveResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public IActionResult Save(string sessionId, [FromBody] SaveSessionRequest request)
    {
        _logger.LogInformation("Saving session {SessionId}, client version {Version}", 
            sessionId, request.ClientVersion);

        var response = _sessionService.SaveChanges(
            sessionId, 
            request.RowData, 
            request.ClientVersion);

        return Ok(response);
    }

    /// <summary>
    /// Reverts a session to a specific version
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="version">Target version to revert to</param>
    /// <returns>New version information and restored data</returns>
    [HttpPost("{sessionId}/revert/{version}")]
    [ProducesResponseType(typeof(RevertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult Revert(string sessionId, int version)
    {
        _logger.LogInformation("Reverting session {SessionId} to version {Version}", 
            sessionId, version);

        var response = _sessionService.RevertToVersion(sessionId, version);

        return Ok(response);
    }

    /// <summary>
    /// Retrieves the complete audit history for a session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <returns>Audit history grouped by version</returns>
    [HttpGet("{sessionId}/audit")]
    [ProducesResponseType(typeof(AuditHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetAudit(string sessionId)
    {
        _logger.LogInformation("Retrieving audit history for session {SessionId}", sessionId);

        var response = _sessionService.GetAuditHistory(sessionId);

        return Ok(response);
    }
}
