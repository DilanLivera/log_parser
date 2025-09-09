using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataProcessor.Pipelines.LogProcessing;

/// <summary>
/// JSON converter for IReadOnlySet&lt;string&gt; to handle serialization and deserialization
/// </summary>
public class ReadOnlySetJsonConverter : JsonConverter<IReadOnlySet<string>>
{
    public override IReadOnlySet<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        List<string>? list = JsonSerializer.Deserialize<List<string>>(ref reader, options);

        return list?.ToHashSet() ?? [];
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlySet<string> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.ToArray(), options);
    }
}