using System.Text.Json.Serialization;

namespace GamebananaAPI.Data.Game.TopSubs;

[JsonSerializable(typeof(List<TopSubmission>))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata)]
internal partial class GameTopSubsJsonContext : JsonSerializerContext;

public class TopSubmission
{
    [JsonPropertyName("_idRow")]
    public int Id { get; set; }

    [JsonPropertyName("_sModelName")]
    public string Type { get; set; }

    [JsonPropertyName("_sName")]
    public string Name { get; set; }

    [JsonPropertyName("_sInitialVisibility")]
    public Visibility SafeForWork { get; set; }

    [JsonPropertyName("_sPeriod")]
    public TopSubmissionPeriod Period { get; set; }

    [JsonPropertyName("_aSubmitter")]
    public SubmissionAuthor Author { get; set; }

    [JsonPropertyName("_sDescription")]
    public string Description { get; set; }

    [JsonPropertyName("_nLikeCount")]
    public int Likes { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter<TopSubmissionPeriod>))]
public enum TopSubmissionPeriod
{
    [JsonStringEnumMemberName("today")]
    Today,

    [JsonStringEnumMemberName("week")]
    ThisWeek,

    [JsonStringEnumMemberName("month")]
    ThisMonth,

    [JsonStringEnumMemberName("3month")]
    ThreeMonths,

    [JsonStringEnumMemberName("6month")]
    SixMonths,

    [JsonStringEnumMemberName("year")]
    ThisYear,

    [JsonStringEnumMemberName("alltime")]
    AllTime,
}
