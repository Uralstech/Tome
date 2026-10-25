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

namespace GamebananaAPI.Data.Categories;

[JsonSerializable(typeof(List<TopLevelCategoryDefinition>))]
[JsonSerializable(typeof(List<CategoryDefinition>))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata)]
internal sealed partial class CategoryJsonContext : JsonSerializerContext;

public class TopLevelCategoryDefinition
{
    [JsonPropertyName("_sPluralTitle")]
    public string PluralTitle { get; set; } = null!;

    [JsonPropertyName("_sModelName")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("_sCategoryModelName")]
    public string CategoryType { get; set; } = null!;
}

public  class CategoryDefinition
{
    [JsonPropertyName("_idRow")]
    public int Id { get; set; }

    [JsonPropertyName("_sName")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("_idParentRow")]
    public int ParentId { get; set; }

    [JsonPropertyName("_aChildren")]
    public List<CategoryDefinition>? Children { get; set; }
}
