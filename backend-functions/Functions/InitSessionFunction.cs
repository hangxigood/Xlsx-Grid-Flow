using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using XlsxGridFlow.Functions.DTOs;
using XlsxGridFlow.Functions.Services;

namespace XlsxGridFlow.Functions.Functions;

/// <summary>
/// Function for initializing a session from template data (e.g., example data)
/// </summary>
public class InitSessionFunction
{
    private readonly ILogger<InitSessionFunction> _logger;
    private readonly BlobSessionService _sessionService;

    public InitSessionFunction(
        ILogger<InitSessionFunction> logger,
        BlobSessionService sessionService)
    {
        _logger = logger;
        _sessionService = sessionService;
    }

    [Function("InitSession")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "template/init")] 
        HttpRequestData req)
    {
        _logger.LogInformation("Processing template init");

        try
        {
            // Parse template from request body
            var template = await req.ReadFromJsonAsync<TemplateDto>();
            
            if (template == null)
            {
                return await CreateErrorResponse(req, "Template data is required", HttpStatusCode.BadRequest);
            }

            _logger.LogInformation("Initializing session for template: {Filename}", template.Filename);

            // Create session
            var (sessionId, expiresAt) = await _sessionService.CreateSessionAsync(template);

            // Return response
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(new
            {
                sessionId,
                expiresAt = expiresAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                template
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing session");
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
