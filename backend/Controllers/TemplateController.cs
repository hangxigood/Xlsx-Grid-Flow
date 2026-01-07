using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using XlsxGridFlow.Api.Configuration;
using XlsxGridFlow.Api.DTOs;
using XlsxGridFlow.Api.DTOs.Responses;
using XlsxGridFlow.Api.Exceptions;
using XlsxGridFlow.Api.Services;

namespace XlsxGridFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TemplateController : ControllerBase
{
    private readonly ExcelService _excelService;
    private readonly SessionService _sessionService;
    private readonly SessionSettings _settings;
    private readonly ILogger<TemplateController> _logger;

    public TemplateController(
        ExcelService excelService,
        SessionService sessionService,
        IOptions<SessionSettings> settings,
        ILogger<TemplateController> logger)
    {
        _excelService = excelService;
        _sessionService = sessionService;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Initializes a session from provided template data (e.g., example data)
    /// </summary>
    /// <param name="template">Template data to initialize session with</param>
    /// <returns>Session information</returns>
    [HttpPost("init")]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public IActionResult InitSession([FromBody] TemplateDto template)
    {
        if (template == null)
        {
            throw new InvalidFileException("Template data is required", "EMPTY_TEMPLATE");
        }

        _logger.LogInformation("Initializing session for template: {Filename}", template.Filename);

        // Create session
        var (sessionId, expiresAt) = _sessionService.CreateSession(template);

        var response = new UploadResponse
        {
            SessionId = sessionId,
            ExpiresAt = expiresAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            Template = template
        };

        return CreatedAtAction(nameof(InitSession), new { id = sessionId }, response);
    }

    /// <summary>
    /// Uploads and parses an Excel file to create a new session
    /// </summary>
    /// <param name="file">Excel file (.xlsx)</param>
    /// <returns>Session information and parsed template</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public IActionResult Upload(IFormFile file)
    {
        // Validate file presence
        if (file == null || file.Length == 0)
        {
            throw new InvalidFileException("No file uploaded or file is empty", "EMPTY_FILE");
        }

        // Validate file type
        var allowedExtensions = new[] { ".xlsx" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidFileException(
                $"Invalid file type. Only {string.Join(", ", allowedExtensions)} files are allowed.");
        }

        // Validate file size
        var maxSizeBytes = _settings.MaxFileSizeMB * 1024 * 1024;
        if (file.Length > maxSizeBytes)
        {
            throw new InvalidFileException(
                $"File size exceeds maximum allowed size of {_settings.MaxFileSizeMB}MB");
        }

        _logger.LogInformation("Processing upload: {Filename} ({Size} bytes)", 
            file.FileName, file.Length);

        // Parse Excel file
        TemplateDto template;
        using (var stream = file.OpenReadStream())
        {
            template = _excelService.ParseExcelFile(stream, file.FileName);
        }

        // Create session
        var (sessionId, expiresAt) = _sessionService.CreateSession(template);

        var response = new UploadResponse
        {
            SessionId = sessionId,
            ExpiresAt = expiresAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            Template = template
        };

        return CreatedAtAction(nameof(Upload), new { id = sessionId }, response);
    }
}
