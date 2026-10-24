using System.Text.Json.Serialization;

namespace Tome.Models;

[JsonSerializable(typeof(TomeMeta))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata)]
internal sealed partial class TomeMetaJsonContext : JsonSerializerContext;

internal sealed class TomeMeta
{
    [JsonPropertyName("backupBy")]
    public required int BackupBy { get; set; }
}
