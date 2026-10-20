using System.Text.Json.Serialization;

namespace Gamebanana.API.Data.Game.Featured;

[JsonSerializable(typeof(FeaturedSubmissions))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization)]
internal partial class GameFeaturedJsonContext : JsonSerializerContext;

public class FeaturedSubmissions
{
    [JsonPropertyName("_aRecords")]
    public List<FeaturedSubmission> Submissions { get; set; }

    [JsonPropertyName("_aMetadata")]
    public SubmissionsMetadata Metadata { get; set; }
}

public class FeaturedSubmission
{
    [JsonPropertyName("_idRow")]
    public string Id { get; set; }

    [JsonPropertyName("_sModelName")]
    public string Type { get; set; }

    [JsonPropertyName("_sName")]
    public string Name { get; set; }

    [JsonPropertyName("_sInitialVisibility")]
    public string SafeForWork { get; set; }

    [JsonPropertyName("_aSubmitter")]
    public SubmissionAuthor Author { get; set; }

    [JsonPropertyName("_sDescription")]
    public string Description { get; set; }

    [JsonPropertyName("_nLikeCount")]
    public int Likes { get; set; }

    [JsonPropertyName("_bHasFiles")]
    public bool HasFiles { get; set; }

    [JsonPropertyName("_bIsObsolete")]
    public bool IsObsolete { get; set; }

    [JsonPropertyName("_aFeaturings")]
    public FeaturingInfo FeaturingInfo { get; set; }
}

public class FeaturingInfo
{
    [JsonPropertyName("_sFeatureGroup")]
    public string Group { get; set; }

    [JsonPropertyName("_sTitle")]
    public string Title { get; set; }
}
