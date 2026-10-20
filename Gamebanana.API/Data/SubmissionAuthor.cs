using System.Text.Json.Serialization;

namespace Gamebanana.API.Data;

public class SubmissionAuthor
{
    [JsonPropertyName("_idRow")]
    public int Id { get; set; }

    [JsonPropertyName("_sName")]
    public string Name { get; set; }

    [JsonPropertyName("_sProfileUrl")]
    public string ProfileUrl { get; set; }
}