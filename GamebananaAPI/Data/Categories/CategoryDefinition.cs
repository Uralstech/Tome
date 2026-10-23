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
