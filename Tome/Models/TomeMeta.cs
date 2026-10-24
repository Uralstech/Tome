using System.Text.Json.Serialization;

namespace Tome.Models;

[JsonSerializable(typeof(TomeMeta))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata)]
internal sealed partial class TomeMetaJsonContext : JsonSerializerContext;

public sealed class TomeMeta
{
    [JsonPropertyName("lastUpdatedBy")]
    public required int LastUpdatedBy { get; set; }
}
