using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using XlsxGridFlow.Functions.DTOs;
using XlsxGridFlow.Functions.Services;

namespace XlsxGridFlow.Functions.Functions;

/// <summary>
/// Function for uploading and parsing Excel templates
/// </summary>
public class UploadTemplateFunction
{
    private readonly ILogger<UploadTemplateFunction> _logger;
    private readonly ExcelService _excelService;
    private readonly BlobSessionService _sessionService;

    public UploadTemplateFunction(
        ILogger<UploadTemplateFunction> logger,
        ExcelService excelService,
        BlobSessionService sessionService)
    {
        _logger = logger;
        _excelService = excelService;
        _sessionService = sessionService;
    }

    [Function("UploadTemplate")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "template/upload")] 
        HttpRequestData req)
    {
        _logger.LogInformation("Processing template upload");

        try
        {
            // Parse multipart form data
            var boundary = GetBoundary(req.Headers);
            if (string.IsNullOrEmpty(boundary))
            {
                return await CreateErrorResponse(req, "Content-Type must be multipart/form-data", HttpStatusCode.BadRequest);
            }

            var formData = await ParseMultipartFormData(req.Body, boundary);
            
            if (formData.FileContent == null || formData.FileContent.Length == 0)
            {
                return await CreateErrorResponse(req, "No file uploaded", HttpStatusCode.BadRequest);
            }

            // Validate file type
            if (!formData.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return await CreateErrorResponse(req, "Only .xlsx files are allowed", HttpStatusCode.BadRequest);
            }

            // Parse Excel file
            using var stream = new MemoryStream(formData.FileContent);
            var template = _excelService.ParseExcelFile(stream, formData.FileName);

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
            _logger.LogError(ex, "Error uploading template");
            return await CreateErrorResponse(req, ex.Message, HttpStatusCode.InternalServerError);
        }
    }

    private string? GetBoundary(HttpHeadersCollection headers)
    {
        if (headers.TryGetValues("Content-Type", out var contentTypes))
        {
            var contentType = contentTypes.FirstOrDefault();
            if (contentType != null && contentType.Contains("boundary="))
            {
                var boundaryIndex = contentType.IndexOf("boundary=") + 9;
                var boundary = contentType.Substring(boundaryIndex);
                return boundary.Trim('"');
            }
        }
        return null;
    }

    private async Task<(byte[] FileContent, string FileName)> ParseMultipartFormData(Stream body, string boundary)
    {
        using var ms = new MemoryStream();
        await body.CopyToAsync(ms);
        var content = ms.ToArray();
        var text = System.Text.Encoding.UTF8.GetString(content);
        
        // Simple multipart parser
        var boundaryBytes = System.Text.Encoding.UTF8.GetBytes("--" + boundary);
        var parts = SplitByBoundary(content, boundaryBytes);
        
        foreach (var part in parts)
        {
            var partText = System.Text.Encoding.UTF8.GetString(part);
            
            // Check if this part contains a file
            if (partText.Contains("filename=\""))
            {
                var filenameMatch = System.Text.RegularExpressions.Regex.Match(partText, @"filename=""([^""]+)""");
                if (filenameMatch.Success)
                {
                    var filename = filenameMatch.Groups[1].Value;
                    
                    // Find the start of file content (after double CRLF)
                    var headerEndIndex = partText.IndexOf("\r\n\r\n");
                    if (headerEndIndex > 0)
                    {
                        var headerBytes = System.Text.Encoding.UTF8.GetByteCount(partText.Substring(0, headerEndIndex + 4));
                        var fileContent = part.Skip(headerBytes).ToArray();
                        
                        // Remove trailing boundary markers
                        var endIndex = fileContent.Length;
                        if (endIndex > 2 && fileContent[endIndex - 2] == '\r' && fileContent[endIndex - 1] == '\n')
                        {
                            fileContent = fileContent.Take(endIndex - 2).ToArray();
                        }
                        
                        return (fileContent, filename);
                    }
                }
            }
        }
        
        return (Array.Empty<byte>(), string.Empty);
    }
    
    private List<byte[]> SplitByBoundary(byte[] content, byte[] boundary)
    {
        var parts = new List<byte[]>();
        var start = 0;
        
        for (int i = 0; i < content.Length - boundary.Length; i++)
        {
            var match = true;
            for (int j = 0; j < boundary.Length && match; j++)
            {
                if (content[i + j] != boundary[j])
                    match = false;
            }
            
            if (match)
            {
                if (i > start)
                {
                    parts.Add(content.Skip(start).Take(i - start).ToArray());
                }
                start = i + boundary.Length;
                i += boundary.Length - 1;
            }
        }
        
        return parts;
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
