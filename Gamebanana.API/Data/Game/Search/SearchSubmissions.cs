using System.Text.Json.Serialization;

namespace GamebananaAPI.Data.Game.Search;

[JsonSerializable(typeof(SearchSubmissions))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata)]
internal sealed partial class GameSearchJsonContext : JsonSerializerContext;

public class SearchSubmissions
{
    [JsonPropertyName("_aRecords")]
    public List<SearchSubmission> Submissions { get; set; } = null!;

    [JsonPropertyName("_aMetadata")]
    public SubmissionsMetadata Metadata { get; set; } = null!;
}

public class SearchSubmission
{
    [JsonPropertyName("_idRow")]
    public int Id { get; set; }

    [JsonPropertyName("_sModelName")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("_sName")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("_sInitialVisibility")]
    public Visibility SafeForWork { get; set; }

    [JsonPropertyName("_aSubmitter")]
    public SubmissionAuthor Author { get; set; } = null!;

    [JsonPropertyName("_nLikeCount")]
    public int Likes { get; set; }

    [JsonPropertyName("_bHasFiles")]
    public bool HasFiles { get; set; }

    [JsonPropertyName("_bIsObsolete")]
    public bool IsObsolete { get; set; }
}
