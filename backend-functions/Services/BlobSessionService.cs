using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using XlsxGridFlow.Functions.DTOs;
using XlsxGridFlow.Functions.Models;

namespace XlsxGridFlow.Functions.Services;

/// <summary>
/// Service for managing sessions in Azure Blob Storage
/// </summary>
public class BlobSessionService
{
    private readonly BlobContainerClient? _containerClient;
    private readonly ILogger<BlobSessionService> _logger;
    private const int SessionExpirationMinutes = 30;
    private const string ContainerName = "sessions";

    public BlobSessionService(BlobServiceClient? blobServiceClient, ILogger<BlobSessionService> logger)
    {
        _logger = logger;
        
        if (blobServiceClient != null)
        {
            _containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
            _containerClient.CreateIfNotExists();
        }
    }

    /// <summary>
    /// Creates a new session from a template
    /// </summary>
    public async Task<(string sessionId, DateTime expiresAt)> CreateSessionAsync(TemplateDto template)
    {
        var sessionId = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.AddMinutes(SessionExpirationMinutes);
        
        var session = new SessionState
        {
            SessionId = sessionId,
            Version = 0,
            Filename = template.Filename,
            ColumnDefs = template.ColumnDefs,
            MergedCells = template.MergedCells,
            CurrentSnapshot = template.RowData,
            ChangeLog = new List<AuditLogEntryDto>(),
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow
        };

        await SaveSessionAsync(sessionId, session);
        
        _logger.LogInformation("Created session {SessionId}, expires at {ExpiresAt}", sessionId, expiresAt);
        
        return (sessionId, expiresAt);
    }

    /// <summary>
    /// Gets a session by ID
    /// </summary>
    public async Task<SessionState?> GetSessionAsync(string sessionId)
    {
        if (_containerClient == null)
        {
            _logger.LogWarning("Blob storage not configured");
            return null;
        }

        var blobClient = _containerClient.GetBlobClient($"{sessionId}.json");
        
        if (!await blobClient.ExistsAsync())
        {
            _logger.LogWarning("Session {SessionId} not found", sessionId);
            return null;
        }

        var download = await blobClient.DownloadContentAsync();
        var session = JsonSerializer.Deserialize<SessionState>(download.Value.Content.ToString());
        
        if (session != null)
        {
            // Update last accessed time
            session.LastAccessedAt = DateTime.UtcNow;
        }

        return session;
    }

    /// <summary>
    /// Updates an existing session
    /// </summary>
    public async Task UpdateSessionAsync(string sessionId, SessionState session)
    {
        session.LastAccessedAt = DateTime.UtcNow;
        await SaveSessionAsync(sessionId, session);
        
        _logger.LogInformation("Updated session {SessionId} to version {Version}", sessionId, session.Version);
    }

    /// <summary>
    /// Saves a session to blob storage
    /// </summary>
    private async Task SaveSessionAsync(string sessionId, SessionState session)
    {
        if (_containerClient == null)
        {
            _logger.LogWarning("Blob storage not configured, session not saved");
            return;
        }

        var blobClient = _containerClient.GetBlobClient($"{sessionId}.json");
        var json = JsonSerializer.Serialize(session);
        
        await blobClient.UploadAsync(
            BinaryData.FromString(json),
            overwrite: true
        );

        // Set metadata for expiration
        var metadata = new Dictionary<string, string>
        {
            ["ExpiresAt"] = DateTime.UtcNow.AddMinutes(SessionExpirationMinutes).ToString("o"),
            ["CreatedAt"] = session.CreatedAt.ToString("o")
        };
        await blobClient.SetMetadataAsync(metadata);
    }

    /// <summary>
    /// Cleans up expired sessions
    /// </summary>
    public async Task<int> CleanupExpiredSessionsAsync()
    {
        if (_containerClient == null)
        {
            _logger.LogWarning("Blob storage not configured");
            return 0;
        }

        var deletedCount = 0;
        var now = DateTime.UtcNow;

        await foreach (var blob in _containerClient.GetBlobsAsync(BlobTraits.Metadata))
        {
            if (blob.Metadata.TryGetValue("ExpiresAt", out var expiresAtStr))
            {
                if (DateTime.TryParse(expiresAtStr, out var expiresAt) && expiresAt < now)
                {
                    await _containerClient.DeleteBlobAsync(blob.Name);
                    deletedCount++;
                    _logger.LogInformation("Deleted expired session blob: {BlobName}", blob.Name);
                }
            }
        }

        return deletedCount;
    }
}
