
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
    /// <summary>Relative path to the vpk file from the 'Deadlock\game\citadel\addons' directory.</summary>
    [JsonPropertyName("relativePath")]
    public required string RelativePath { get; set; }

    /// <summary>The original MD5 file checksum.</summary>
    [JsonPropertyName("originalFileMd5Checksum")]
    public required string OriginalFileMd5Checksum { get; set; } = null!;

    /// <summary>Gamebanana submission metadata. If <see langword="null"/>, this is a local mod.</summary>
    [JsonPropertyName("gamebananaMetadata")]
    public GamebananaMetadata? GamebananaMetadata { get; set; }

    /// <summary>Optional metadata that may be displayed in Tome's GUI.</summary>
    [JsonPropertyName("listingMetadata")]
    public ListingMetadata? ListingMetadata { get; set; }
}

public sealed class GamebananaMetadata
{
    /// <summary>The Gamebanana submission ID.</summary>
    [JsonPropertyName("submissionId")]
    public required int SubmissionId { get; set; }

    /// <summary>The Gamebanana submission type (Mod/Sound).</summary>
    [JsonPropertyName("submissionType")]
    public required int SubmissionType { get; set; }

    /// <summary>The Gamebanana submission category.</summary>
    [JsonPropertyName("submissionCategoryType")]
    public required string SubmissionCategoryType { get; set; }

    /// <summary>The full Gamebanana submission category (ID) tree.</summary>
    [JsonPropertyName("submissionCategoryTree")]
    public required IReadOnlyList<int> SubmissionCategoryTree { get; set; }

    /// <summary>The Gamebanana file ID.</summary>
    [JsonPropertyName("fileId")]
    public required int FileId { get; set; }

    /// <summary>The original file name from Gamebanana.</summary>
    [JsonPropertyName("fileName")]
    public required string FileName { get; set; }

    /// <summary>The date the file was uploaded to Gamebanana.</summary>
    [JsonPropertyName("fileUploadDate")]
    public required DateTime FileUploadDate { get; set; }
}

/// <summary>Optional mod metadata that may be displayed in Tome's GUI.</summary>
public sealed class ListingMetadata
{
    /// <summary>The user-readable name of the mod.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The user-readable name of the mod's author.</summary>
    public string Author { get; init; } = string.Empty;
}
