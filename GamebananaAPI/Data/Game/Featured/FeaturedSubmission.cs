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

namespace GamebananaAPI.Data.Game.Featured;

[JsonSerializable(typeof(FeaturedSubmissions))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata)]
internal sealed partial class GameFeaturedJsonContext : JsonSerializerContext;

public class FeaturedSubmissions
{
    [JsonPropertyName("_aRecords")]
    public List<FeaturedSubmission> Submissions { get; set; } = null!;

    [JsonPropertyName("_aMetadata")]
    public SubmissionsMetadata Metadata { get; set; } = null!;
}

public class FeaturedSubmission
{
    [JsonPropertyName("_idRow")]
    public int Id { get; set; }

    [JsonPropertyName("_sModelName")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("_sName")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("_sInitialVisibility")]
    public Visibility SafeForWork { get; set; }

    [JsonPropertyName("_aSubmitter")]
    public SubmissionAuthor Author { get; set; } = null!;

    [JsonPropertyName("_sDescription")]
    public string? Description { get; set; }

    [JsonPropertyName("_nLikeCount")]
    public int Likes { get; set; }

    [JsonPropertyName("_bHasFiles")]
    public bool HasFiles { get; set; }

    [JsonPropertyName("_bIsObsolete")]
    public bool IsObsolete { get; set; }

    [JsonPropertyName("_aFeaturings")]
    public List<FeaturingInfo> Featurings { get; set; } = null!;
}

public class FeaturingInfo
{
    [JsonPropertyName("_sFeatureGroup")]
    public FeaturingGroup Group { get; set; }

    [JsonPropertyName("_sTitle")]
    public string Title { get; set; } = null!;

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
