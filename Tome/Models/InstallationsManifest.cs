
using System.Text.Json.Serialization;

namespace Tome.Models;

[JsonSerializable(typeof(InstallationsManifest))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata)]
internal sealed partial class InstallationsManifestJsonContext : JsonSerializerContext;

/// <summary>Managed mod installations.</summary>
public sealed class InstallationsManifest
{
    /// <summary>Path to the game folder.</summary>
    [JsonPropertyName("gamePath")]
    public required string GamePath { get; set; }

    /// <summary>Installed skins where the key is the ritualist category ID (eg. 42671 for Silver, 49631 for Violet).</summary>
    /// <remarks>Since each ritualist can have multiple mods (eg. recolor mod + 3D model mod), the dictionary value is a list.</remarks>
    [JsonPropertyName("skins")]
    public Dictionary<int, List<InstalledMod>> Skins { get; set; } = [];

    /// <summary>Installed model replacements where the key is the model replacement (sub-)category ID (eg. 41651 for breakables, 44687 for troopers).</summary>
    /// <remarks>Since each category can have multiple mods (eg. hidden king mod + archmother mod, both under the "patron" sub-category), the dictionary value is a list.</remarks>
    [JsonPropertyName("modelReplacements")]
    public Dictionary<int, List<InstalledMod>> ModelReplacements { get; set; } = [];

    /// <summary>Any mods that aren't skins or model replacements, and likely don't conflict with each other.</summary>
    [JsonPropertyName("misc")]
    public List<InstalledMod> Misc { get; set; } = [];
}

/// <summary>Entry for an installed mod.</summary>
public sealed class InstalledMod
{
    /// <summary>Path to the vpk file.</summary>
    [JsonPropertyName("path")]
    public required string Path { get; set; }

    /// <summary>The Gamebanana submission ID.</summary>
    [JsonPropertyName("originalModId")]
    public required int OriginalModId { get; set; }

    /// <summary>The Gamebanana submission type (Mod/Sound).</summary>
    [JsonPropertyName("originalModType")]
    public required int OriginalModType { get; set; }

    /// <summary>The Gamebanana submission category.</summary>
    [JsonPropertyName("originalModCategoryType")]
    public required string OriginalModCategoryType { get; set; }

    /// <summary>The full Gamebanana submission category (ID) tree.</summary>
    [JsonPropertyName("originalModCategoryTree")]
    public required IReadOnlyList<int> OriginalModCategoryTree { get; set; }

    /// <summary>The Gamebanana file ID.</summary>
    [JsonPropertyName("originalFileId")]
    public required int OriginalFileId { get; set; }

    /// <summary>The original file name from Gamebanana.</summary>
    [JsonPropertyName("originalFileName")]
    public required string OriginalFileName { get; set; }

    /// <summary>The date the file was uploaded to Gamebanana.</summary>
    [JsonPropertyName("originalFileUploadDate")]
    public required DateTime OriginalFileUploadDate { get; set; }

    /// <summary>Optional metadata that may be displayed in Tome's GUI.</summary>
    [JsonPropertyName("listingMetadata")]
    public ListingMetadata? ListingMetadata { get; set; }
}


/// <summary>Optional mod metadata that may be displayed in Tome's GUI.</summary>
public sealed class ListingMetadata
{
    /// <summary>The user-readable name of the mod.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The user-readable name of the mod's author.</summary>
    public string Author { get; init; } = string.Empty;
}
