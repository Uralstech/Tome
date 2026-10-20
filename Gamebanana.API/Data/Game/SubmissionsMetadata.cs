using System.Text.Json.Serialization;

namespace Gamebanana.API.Data.Game;

public class SubmissionsMetadata
{
    [JsonPropertyName("_nRecordCount")]
    public int RecordsCount { get; set; }

    [JsonPropertyName("_nPerpage")]
    public int RecordsPerPage { get; set; }

    [JsonPropertyName("_bIsComplete")]
    public bool IsComplete { get; set;}
}

public class SubmissionAuthor
{
    [JsonPropertyName("_idRow")]
    public string Id { get; set; }

    [JsonPropertyName("_sName")]
    public string Name { get; set; }
}