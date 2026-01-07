using System.Text.Json;

namespace XlsxGridFlow.Api.Utilities;

/// <summary>
/// Helper methods for JSON processing
/// </summary>
public static class JsonHelper
{
    /// <summary>
    /// Extracts the actual value from a JsonElement or returns the value as-is
    /// </summary>
    public static object? ExtractValue(object? value)
    {
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString(),
                JsonValueKind.Number => jsonElement.TryGetInt32(out var intVal) 
                    ? intVal 
                    : jsonElement.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => jsonElement.ToString()
            };
        }
        return value;
    }
}
