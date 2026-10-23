using System.Text.Json.Serialization;

namespace GamebananaAPI.Data;

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