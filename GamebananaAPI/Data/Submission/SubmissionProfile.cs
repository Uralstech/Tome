// Copyright 2026 URAV ADVANCED LEARNING SYSTEMS PRIVATE LIMITED
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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

    [JsonPropertyName("_sInitialVisibility")]
    public Visibility SafeForWork { get; set; }

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
    public LicenseChecklist? LicenseChecklist { get; set; }

    [JsonPropertyName("_aFiles")]
    public List<SubmissionFile>? Files { get; set; }

    [JsonPropertyName("_sDescription")]
    public string? Description { get; set; }

    [JsonPropertyName("_sText")]
    public string? Text { get; set; }

    [JsonPropertyName("_aCategory")]
    public Category? Category { get; set; }

    [JsonPropertyName("_aSuperCategory")]
    public Category? SuperCategory { get; set; }

    [JsonPropertyName("_aCredits")]
    public List<CreditsGroup>? Credits { get; set; } = null!;
}

public class Category
{
    [JsonPropertyName("_idRow")]
    public int Id { get; set; }

    [JsonPropertyName("_sName")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("_sModelName")]
    public string Type { get; set; } = null!;
}

public class CreditsGroup
{
    [JsonPropertyName("_sGroupName")]
    public string GroupName { get; set; } = null!;

    [JsonPropertyName("_aAuthors")]
    public List<CreditedAuthor> Authors { get; set; } = null!;
}

public class CreditedAuthor
{
    [JsonPropertyName("_sName")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("_sRole")]
    public string? Role { get; set; }

    [JsonPropertyName("_sProfileUrl")]
    public string? ProfileUrl { get; set; }

    [JsonPropertyName("_sUrl")]
    public string? OtherUrl { get; set; }
}

public class LicenseChecklist
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