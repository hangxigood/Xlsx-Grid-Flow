namespace XlsxGridFlow.Api.Exceptions;

/// <summary>
/// Base exception for application-specific errors
/// </summary>
public class AppException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    public AppException(string errorCode, string message, int statusCode = 400) 
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}

/// <summary>
/// Exception thrown when session is not found or expired
/// </summary>
public class SessionNotFoundException : AppException
{
    public SessionNotFoundException(string sessionId) 
        : base("SESSION_NOT_FOUND", $"Session '{sessionId}' not found or expired", 404)
    {
    }
}

/// <summary>
/// Exception thrown when version conflict occurs
/// </summary>
public class ConcurrencyConflictException : AppException
{
    public ConcurrencyConflictException(int clientVersion, int serverVersion) 
        : base("CONCURRENCY_CONFLICT", 
               $"Version conflict: client version {clientVersion} does not match server version {serverVersion}", 
               409)
    {
    }
}

/// <summary>
/// Exception thrown when version is not found
/// </summary>
public class VersionNotFoundException : AppException
{
    public VersionNotFoundException(int version) 
        : base("VERSION_NOT_FOUND", $"Version {version} not found", 404)
    {
    }
}

/// <summary>
/// Exception thrown when file validation fails
/// </summary>
public class InvalidFileException : AppException
{
    public InvalidFileException(string message, string errorCode = "INVALID_FILE_TYPE") 
        : base(errorCode, message, 400)
    {
    }
}

/// <summary>
/// Exception thrown when Excel parsing fails
/// </summary>
public class ParsingException : AppException
{
    public ParsingException(string message) 
        : base("PARSING_ERROR", message, 400)
    {
    }
}

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : AppException
{
    public Dictionary<string, string>? ValidationErrors { get; }

    public ValidationException(string message, Dictionary<string, string>? errors = null) 
        : base("VALIDATION_FAILED", message, 400)
    {
        ValidationErrors = errors;
    }
}
