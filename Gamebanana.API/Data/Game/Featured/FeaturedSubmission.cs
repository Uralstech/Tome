using System.Text.Json.Serialization;

namespace GamebananaAPI.Data.Game.Featured;

[JsonSerializable(typeof(FeaturedSubmissions))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata)]
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
    public int Id { get; set; }

    [JsonPropertyName("_sModelName")]
    public string Type { get; set; }

    [JsonPropertyName("_sName")]
    public string Name { get; set; }

    [JsonPropertyName("_sInitialVisibility")]
    public Visibility SafeForWork { get; set; }

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
    public List<FeaturingInfo> Featurings { get; set; }
}

public class FeaturingInfo
{
    [JsonPropertyName("_sFeatureGroup")]
    public FeaturingGroup Group { get; set; }

    [JsonPropertyName("_sTitle")]
    public string Title { get; set; }

    [JsonPropertyName("_tsDate")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTime Date { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter<FeaturingGroup>))]
public enum FeaturingGroup
{
    [JsonStringEnumMemberName("today")]
    Today,

    [JsonStringEnumMemberName("yesterday")]
    Yesterday,

    [JsonStringEnumMemberName("3day")]
    ThreeDays
}
