using System.Text.Json.Serialization;

namespace XlsxGridFlow.Functions.Models;

/// <summary>
/// Supported data types for grid columns
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DataType
{
    Text,
    Number,
    Date,
    Boolean,
    Formula
}
