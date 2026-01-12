using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using XlsxGridFlow.Api.Configuration;
using XlsxGridFlow.Api.DTOs;
using XlsxGridFlow.Api.DTOs.Responses;
using XlsxGridFlow.Api.Exceptions;
using XlsxGridFlow.Api.Models;

namespace XlsxGridFlow.Api.Services;

/// <summary>
/// Service for managing in-memory sessions
/// </summary>
public class SessionService
{
    private readonly IMemoryCache _cache;
    private readonly DiffService _diffService;
    private readonly FormulaService _formulaService;
    private readonly SessionSettings _settings;
    private readonly ILogger<SessionService> _logger;

    public SessionService(
        IMemoryCache cache,
        DiffService diffService,
        FormulaService formulaService,
        IOptions<SessionSettings> settings,
        ILogger<SessionService> logger)
    {
        _cache = cache;
        _diffService = diffService;
        _formulaService = formulaService;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new session from parsed template
    /// </summary>
    public (string sessionId, DateTime expiresAt) CreateSession(TemplateDto template)
    {
        var sessionId = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes);

        // Recalculate formulas to store calculated results instead of formula strings
        var initialData = _formulaService.RecalculateFormulas(template.ColumnDefs, template.RowData);

        var sessionState = new SessionState
        {
            SessionId = sessionId,
            Version = 0,
            Filename = template.Filename,
            ColumnDefs = template.ColumnDefs,
            MergedCells = template.MergedCells,
            CurrentSnapshot = DeepCopyRowData(initialData),
            ChangeLog = new List<AuditLogEntryDto>(),
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow
        };

        var cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(_settings.ExpirationMinutes)
        };

        _cache.Set(GetCacheKey(sessionId), sessionState, cacheOptions);

        _logger.LogInformation("Created session {SessionId}, expires at {ExpiresAt}", 
            sessionId, expiresAt);

        return (sessionId, expiresAt);
    }

    /// <summary>
    /// Retrieves a session by ID
    /// </summary>
    public SessionState GetSession(string sessionId)
    {
        if (!_cache.TryGetValue(GetCacheKey(sessionId), out SessionState? session) || session == null)
        {
            throw new SessionNotFoundException(sessionId);
        }

        session.LastAccessedAt = DateTime.UtcNow;
        return session;
    }

    /// <summary>
    /// Saves changes to a session and creates a new version
    /// </summary>
    public SaveResponse SaveChanges(string sessionId, List<GridRowDto> newData, int clientVersion)
    {
        var session = GetSession(sessionId);

        // Optimistic concurrency check
        if (session.Version != clientVersion)
        {
            throw new ConcurrencyConflictException(clientVersion, session.Version);
        }

        // Recalculate all formulas with the new data
        // This ensures formulas are up-to-date based on user's editable cell changes
        var recalculatedData = _formulaService.RecalculateFormulas(session.ColumnDefs, newData);

        // Calculate diff (only editable cells will have changed, formulas stay the same)
        var newVersion = session.Version + 1;
        var auditEntries = _diffService.CalculateDiff(
            session.CurrentSnapshot,
            recalculatedData,
            session.ColumnDefs,
            newVersion);

        // Update session with recalculated data
        session.Version = newVersion;
        session.CurrentSnapshot = DeepCopyRowData(recalculatedData);
        session.ChangeLog.AddRange(auditEntries);
        session.LastAccessedAt = DateTime.UtcNow;

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        _logger.LogInformation("Saved session {SessionId} as version {Version} with {ChangeCount} changes",
            sessionId, newVersion, auditEntries.Count);

        return new SaveResponse
        {
            NewVersion = newVersion,
            Timestamp = timestamp,
            AuditEntries = auditEntries
        };
    }

    /// <summary>
    /// Reverts session to a specific version
    /// </summary>
    public RevertResponse RevertToVersion(string sessionId, int targetVersion)
    {
        var session = GetSession(sessionId);

        // Validate target version exists
        var targetVersionEntries = session.ChangeLog.Where(e => e.Version == targetVersion).ToList();
        if (targetVersion > 0 && !targetVersionEntries.Any())
        {
            throw new VersionNotFoundException(targetVersion);
        }

        // Reconstruct data at target version
        var revertedData = ReconstructDataAtVersion(session, targetVersion);

        // Calculate diff for the revert action
        var newVersion = session.Version + 1;
        var auditEntries = _diffService.CalculateDiff(
            session.CurrentSnapshot,
            revertedData,
            session.ColumnDefs,
            newVersion);

        // Update session
        session.Version = newVersion;
        session.CurrentSnapshot = DeepCopyRowData(revertedData);
        session.ChangeLog.AddRange(auditEntries);
        session.LastAccessedAt = DateTime.UtcNow;

        _logger.LogInformation("Reverted session {SessionId} to version {TargetVersion}, created version {NewVersion}",
            sessionId, targetVersion, newVersion);

        return new RevertResponse
        {
            NewVersion = newVersion,
            RowData = revertedData,
            AuditEntries = auditEntries
        };
    }

    /// <summary>
    /// Gets the complete audit history for a session
    /// </summary>
    public AuditHistoryResponse GetAuditHistory(string sessionId)
    {
        var session = GetSession(sessionId);

        var history = session.ChangeLog
            .GroupBy(e => e.Version)
            .OrderBy(g => g.Key)
            .Select(g => new VersionHistory
            {
                Version = g.Key,
                Timestamp = g.First().Timestamp,
                Entries = g.ToList()
            })
            .ToList();

        return new AuditHistoryResponse
        {
            SessionId = sessionId,
            History = history
        };
    }

    /// <summary>
    /// Reconstructs data at a specific version by replaying changes
    /// </summary>
    private List<GridRowDto> ReconstructDataAtVersion(SessionState session, int targetVersion)
    {
        // Start with empty data
        var data = new Dictionary<int, Dictionary<string, object?>>();

        // Replay all changes up to target version
        var relevantEntries = session.ChangeLog
            .Where(e => e.Version <= targetVersion)
            .OrderBy(e => e.Version)
            .ThenBy(e => e.Timestamp);

        foreach (var entry in relevantEntries)
        {
            var (rowId, field) = ParseCellReference(entry.CellReference);

            if (!data.ContainsKey(rowId))
            {
                data[rowId] = new Dictionary<string, object?>();
            }

            data[rowId][field] = entry.NewValue;
        }

        // Convert to GridRowDto list
        return data.Select(kvp => new GridRowDto
        {
            RowId = kvp.Key,
            Cells = new Dictionary<string, object?>(kvp.Value)
        }).OrderBy(r => r.RowId).ToList();
    }

    /// <summary>
    /// Parses cell reference (e.g., "B4") to row ID and column field
    /// </summary>
    private (int rowId, string field) ParseCellReference(string cellRef)
    {
        var coord = Utilities.CoordinateMapper.ParseCellReference(cellRef);
        
        if (coord == null)
        {
            throw new ArgumentException($"Invalid cell reference: {cellRef}");
        }

        return (coord.Row, coord.Column);
    }

    /// <summary>
    /// Deep copies row data to prevent reference issues
    /// </summary>
    private List<GridRowDto> DeepCopyRowData(List<GridRowDto> source)
    {
        return source.Select(row => new GridRowDto
        {
            RowId = row.RowId,
            Cells = new Dictionary<string, object?>(row.Cells)
        }).ToList();
    }

    /// <summary>
    /// Gets the cache key for a session
    /// </summary>
    private string GetCacheKey(string sessionId) => $"session:{sessionId}";
}
