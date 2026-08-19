using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stedi.Healthcare.Serialization;

/// <summary>
/// Central <see cref="System.Text.Json"/> settings used by the SDK.
/// </summary>
public static class StediJsonSerializer
{
    /// <summary>Serializer options for Stedi JSON payloads.</summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = false,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            WriteIndented = false,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    /// <summary>Serializes a value using SDK settings.</summary>
    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    /// <summary>Deserializes a value using SDK settings.</summary>
    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);

    /// <summary>Deserializes a value using SDK settings.</summary>
    public static T? Deserialize<T>(Stream stream) => JsonSerializer.Deserialize<T>(stream, Options);
}
