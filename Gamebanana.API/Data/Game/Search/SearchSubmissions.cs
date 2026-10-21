using System.Text.Json.Serialization;

namespace GamebananaAPI.Data.Game.Search;

[JsonSerializable(typeof(SearchSubmissions))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata)]
internal partial class GameSearchJsonContext : JsonSerializerContext;

public class SearchSubmissions
{
    [JsonPropertyName("_aRecords")]
    public List<SearchSubmission> Submissions { get; set; }

    [JsonPropertyName("_aMetadata")]
    public SubmissionsMetadata Metadata { get; set; }
}

public class SearchSubmission
{
    [JsonPropertyName("_idRow")]
    public int Id { get; set; }

    [JsonPropertyName("_sModelName")]
    public string Type { get; set; }

    [JsonPropertyName("_sName")]
    public string Name { get; set; }

    [JsonPropertyName("_sInitialVisibility")]
    public Visibility SafeForWork { get; set; }

    [JsonPropertyName("_aSubmitter")]
    public SubmissionAuthor Author { get; set; }

    [JsonPropertyName("_nLikeCount")]
    public int Likes { get; set; }

    [JsonPropertyName("_bHasFiles")]
    public bool HasFiles { get; set; }

    [JsonPropertyName("_bIsObsolete")]
    public bool IsObsolete { get; set; }
}
