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

namespace GamebananaAPI.Data.Game.TopSubs;

[JsonSerializable(typeof(List<TopSubmission>))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata)]
internal sealed partial class GameTopSubsJsonContext : JsonSerializerContext;

public class TopSubmission
{
    [JsonPropertyName("_idRow")]
    public int Id { get; set; }

    [JsonPropertyName("_sModelName")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("_sName")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("_sInitialVisibility")]
    public Visibility SafeForWork { get; set; }

    [JsonPropertyName("_sPeriod")]
    public TopSubmissionPeriod Period { get; set; }

    [JsonPropertyName("_aSubmitter")]
    public SubmissionAuthor Author { get; set; } = null!;

    [JsonPropertyName("_sDescription")]
    public string? Description { get; set; }

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
