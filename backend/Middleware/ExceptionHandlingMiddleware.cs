using System.Net;
using System.Text.Json;
using XlsxGridFlow.Api.DTOs.Responses;
using XlsxGridFlow.Api.Exceptions;

namespace XlsxGridFlow.Api.Middleware;

/// <summary>
/// Global exception handling middleware
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An error occurred: {Message}", exception.Message);

        var response = context.Response;
        response.ContentType = "application/json";

        ErrorResponse errorResponse;

        switch (exception)
        {
            case AppException appEx:
                response.StatusCode = appEx.StatusCode;
                errorResponse = new ErrorResponse
                {
                    ErrorCode = appEx.ErrorCode,
                    Message = appEx.Message,
                    Details = appEx is ValidationException valEx ? valEx.ValidationErrors : null
                };
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse = new ErrorResponse
                {
                    ErrorCode = "SERVER_ERROR",
                    Message = "An unexpected error occurred"
                };
                break;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await response.WriteAsync(JsonSerializer.Serialize(errorResponse, jsonOptions));
    }
}
