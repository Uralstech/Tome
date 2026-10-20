using System.Text.Json.Serialization;

namespace Gamebanana.API.Data.Game.TopSubs;

[JsonSerializable(typeof(List<TopSubmission>))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization)]
internal partial class GameTopSubsJsonContext : JsonSerializerContext;

public class TopSubmission
{
    [JsonPropertyName("_idRow")]
    public string Id { get; set; }

    [JsonPropertyName("_sModelName")]
    public string Type { get; set; }

    [JsonPropertyName("_sName")]
    public string Name { get; set; }

    [JsonPropertyName("_sInitialVisibility")]
    public string SafeForWork { get; set; }

    [JsonPropertyName("_sPeriod")]
    public string Period { get; set; }

    [JsonPropertyName("_aSubmitter")]
    public SubmissionAuthor Author { get; set; }

    [JsonPropertyName("_sDescription")]
    public string Description { get; set; }

    [JsonPropertyName("_nLikeCount")]
    public int Likes { get; set; }
}
