using XlsxGridFlow.Functions.DTOs;

namespace XlsxGridFlow.Functions.Utilities;

/// <summary>
/// Helper utilities for working with merged cells
/// </summary>
public static class MergedCellHelper
{
    /// <summary>
    /// Creates a lookup dictionary for merged cells to quickly check if a cell is part of a merged range
    /// </summary>
    /// <param name="mergedCells">List of merged cell ranges</param>
    /// <returns>Dictionary with key "rowId,colIndex" mapping to the merged cell range</returns>
    public static Dictionary<string, MergedCellDto> CreateMergedCellLookup(List<MergedCellDto> mergedCells)
    {
        var lookup = new Dictionary<string, MergedCellDto>();
        
        foreach (var merged in mergedCells)
        {
            for (int r = merged.StartRow; r <= merged.EndRow; r++)
            {
                for (int c = merged.StartCol; c <= merged.EndCol; c++)
                {
                    lookup[$"{r},{c}"] = merged;
                }
            }
        }
        
        return lookup;
    }

    /// <summary>
    /// Checks if a cell is the top-left cell of a merged range
    /// </summary>
    public static bool IsTopLeftOfMergedRange(int rowId, int colIndex, MergedCellDto mergedCell)
    {
        return rowId == mergedCell.StartRow && colIndex == mergedCell.StartCol;
    }

    /// <summary>
    /// Calculates the row span for a merged cell
    /// </summary>
    public static uint GetRowSpan(MergedCellDto mergedCell)
    {
        return (uint)(mergedCell.EndRow - mergedCell.StartRow + 1);
    }

    /// <summary>
    /// Calculates the column span for a merged cell
    /// </summary>
    public static uint GetColumnSpan(MergedCellDto mergedCell)
    {
        return (uint)(mergedCell.EndCol - mergedCell.StartCol + 1);
    }
}
