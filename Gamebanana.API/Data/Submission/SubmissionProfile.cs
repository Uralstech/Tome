using System.Text.Json.Serialization;

namespace GamebananaAPI.Data.Submission;

[JsonSerializable(typeof(SubmissionProfile))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata)]
internal sealed partial class SubmissionJsonContext : JsonSerializerContext;

public class SubmissionProfile
{
    [JsonPropertyName("_idRow")]
    public int Id { get; set; }

    [JsonPropertyName("_tsDateAdded")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTime Added { get; set; }

    [JsonPropertyName("_tsDateModified")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTime? Modified { get; set; }

    [JsonPropertyName("_tsDateUpdated")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTime? Updated { get; set; }

    [JsonPropertyName("_sName")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("_bIsNsfw")]
    public bool IsNotSafeForWork { get; set; }

    [JsonPropertyName("_sProfileUrl")]
    public string ProfileUrl { get; set; } = null!;

    [JsonPropertyName("_nSubscriberCount")]
    public int Subscribers { get; set; }

    [JsonPropertyName("_nThanksCount")]
    public int Thanks { get; set; }

    [JsonPropertyName("_nDownloadCount")]
    public int Downloads { get; set; }

    [JsonPropertyName("_nViewCount")]
    public int Views { get; set; }

    [JsonPropertyName("_nLikeCount")]
    public int Likes { get; set; }

    [JsonPropertyName("_aSubmitter")]
    public SubmissionAuthor Author { get; set; } = null!;

    [JsonPropertyName("_aStudio")]
    public SubmissionAuthor? Studio { get; set; }

    [JsonPropertyName("_bIsObsolete")]
    public bool IsObsolete { get; set; }

    [JsonPropertyName("_sLicense")]
    public string LicenseHtml { get; set; } = null!;

    [JsonPropertyName("_aLicenseChecklist")]
    public SubmissionLicenseChecklist? LicenseChecklist { get; set; }

    [JsonPropertyName("_aFiles")]
    public List<SubmissionFile>? Files { get; set; }
}

public class SubmissionLicenseChecklist
{
    [JsonPropertyName("yes")]
    public string[]? Yes { get; set; }

    [JsonPropertyName("ask")]
    public string[]? Ask { get; set; }

    [JsonPropertyName("no")]
    public string[]? No { get; set; }
}

public class SubmissionFile
{
    [JsonPropertyName("_idRow")]
    public int Id { get; set; }

    [JsonPropertyName("_sFile")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("_sDescription")]
    public string? Description { get; set; }

    [JsonPropertyName("_sVersion")]
    public string? Version { get; set; }

    [JsonPropertyName("_nFilesize")]
    public long Size { get; set; }

    [JsonPropertyName("_tsDateAdded")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTime Added { get; set; }

    [JsonPropertyName("_nDownloadCount")]
    public int Downloads { get; set; }

    [JsonPropertyName("_sDownloadUrl")]
    public string DownloadUrl { get; set; } = null!;

    [JsonPropertyName("_sMd5Checksum")]
    public string Md5Checksum { get; set; } = null!;
}