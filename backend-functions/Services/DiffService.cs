using Microsoft.Extensions.Logging;
using XlsxGridFlow.Functions.DTOs;
using XlsxGridFlow.Functions.Utilities;

namespace XlsxGridFlow.Functions.Services;

/// <summary>
/// Service for calculating differences between grid snapshots
/// </summary>
public class DiffService
{
    private readonly ILogger<DiffService> _logger;

    public DiffService(ILogger<DiffService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculates the difference between two snapshots and generates audit entries
    /// </summary>
    public List<AuditLogEntryDto> CalculateDiff(
        List<GridRowDto> oldData,
        List<GridRowDto> newData,
        List<ColumnDefDto> columnDefs,
        int version)
    {
        var auditEntries = new List<AuditLogEntryDto>();
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        // Create lookup dictionaries for faster access
        var oldRowsDict = oldData.ToDictionary(r => r.RowId);
        var newRowsDict = newData.ToDictionary(r => r.RowId);

        // Get all row IDs from both snapshots
        var allRowIds = oldRowsDict.Keys.Union(newRowsDict.Keys).OrderBy(id => id);

        foreach (var rowId in allRowIds)
        {
            var hasOldRow = oldRowsDict.TryGetValue(rowId, out var oldRow);
            var hasNewRow = newRowsDict.TryGetValue(rowId, out var newRow);

            if (!hasOldRow && hasNewRow)
            {
                // New row added - log all cells (including formula results)
                foreach (var colDef in columnDefs)
                {
                    if (newRow!.Cells.TryGetValue(colDef.Field, out var newValue) && newValue != null)
                    {
                        auditEntries.Add(new AuditLogEntryDto
                        {
                            Version = version,
                            Timestamp = timestamp,
                            CellReference = GetCellReference(rowId, colDef.Field),
                            OldValue = null,
                            NewValue = newValue
                        });
                    }
                }
            }
            else if (hasOldRow && !hasNewRow)
            {
                // Row deleted - log all cells (including formula results)
                foreach (var colDef in columnDefs)
                {
                    if (oldRow!.Cells.TryGetValue(colDef.Field, out var oldValue) && oldValue != null)
                    {
                        auditEntries.Add(new AuditLogEntryDto
                        {
                            Version = version,
                            Timestamp = timestamp,
                            CellReference = GetCellReference(rowId, colDef.Field),
                            OldValue = oldValue,
                            NewValue = null
                        });
                    }
                }
            }
            else if (hasOldRow && hasNewRow)
            {
                // Compare cells in existing row - log ALL changes including formula results
                foreach (var colDef in columnDefs)
                {
                    oldRow!.Cells.TryGetValue(colDef.Field, out var oldValue);
                    newRow!.Cells.TryGetValue(colDef.Field, out var newValue);

                    if (!AreValuesEqual(oldValue, newValue))
                    {
                        _logger.LogDebug("Found diff: {RowId}, {ColumnField}, Old: {OldValue}, New: {NewValue}", 
                            rowId, colDef.Field, oldValue, newValue);
                        auditEntries.Add(new AuditLogEntryDto
                        {
                            Version = version,  
                            Timestamp = timestamp,
                            CellReference = GetCellReference(rowId, colDef.Field),
                            OldValue = oldValue,
                            NewValue = newValue
                        });
                    }
                }
            }
        }

        _logger.LogInformation("Calculated diff: {Count} changes for version {Version}", 
            auditEntries.Count, version);
 
        return auditEntries;
    }

    /// <summary>
    /// Compares two cell values for equality
    /// </summary>
    private bool AreValuesEqual(object? value1, object? value2)
    {
        if (value1 == null && value2 == null) return true;
        if (value1 == null || value2 == null) return false;

        // Extract actual values from JsonElement if needed
        var actualValue1 = JsonHelper.ExtractValue(value1);
        var actualValue2 = JsonHelper.ExtractValue(value2);

        if (actualValue1 == null && actualValue2 == null) return true;
        if (actualValue1 == null || actualValue2 == null) return false;

        // Handle numeric comparisons
        if (IsNumeric(actualValue1) && IsNumeric(actualValue2))
        {
            return Convert.ToDouble(actualValue1) == Convert.ToDouble(actualValue2);
        }

        // String comparison (case-sensitive)
        return actualValue1.ToString() == actualValue2.ToString();
    }

    /// <summary>
    /// Checks if a value is numeric
    /// </summary>
    private bool IsNumeric(object value)
    {
        return value is int or long or float or double or decimal;
    }

    /// <summary>
    /// Converts row ID and column field to Excel cell reference (e.g., "B4")
    /// </summary>
    private string GetCellReference(int rowId, string columnField)
    {
        return $"{columnField}{rowId}";
    }
}
