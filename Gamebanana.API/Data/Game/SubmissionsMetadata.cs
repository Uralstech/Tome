using System.Text.Json.Serialization;

namespace GamebananaAPI.Data.Game;

public class SubmissionsMetadata
{
    [JsonPropertyName("_nRecordCount")]
    public int RecordsCount { get; set; }

    [JsonPropertyName("_nPerpage")]
    public int RecordsPerPage { get; set; }

    [JsonPropertyName("_bIsComplete")]
    public bool IsComplete { get; set;}
}

[JsonConverter(typeof(JsonStringEnumConverter<Visibility>))]
public enum Visibility
{
    [JsonStringEnumMemberName("show")]
    Show,

    [JsonStringEnumMemberName("warn")]
    Warn,

    [JsonStringEnumMemberName("hide")]
    Hide
}