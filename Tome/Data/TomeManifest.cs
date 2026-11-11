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

using GamebananaAPI.Data;
using System.Text.Json.Serialization;

namespace Tome.Data;

/// <summary>Managed mod installations.</summary>
public sealed class TomeManifest
{
    /// <summary>Path to the game folder.</summary>
    [JsonPropertyName("gamePath")]
    public required string GamePath { get; set; }

    /// <summary>Default filters to use in search, featured, top submissions.</summary>
    [JsonPropertyName("defaultFilters")]
    public Visibility DefaultFilters { get; set; } = Visibility.Show;

    [JsonPropertyName("mods"), Obsolete]
    public Dictionary<int, Legacy.InstalledMod> Mods { get; set; } = [];

    [JsonPropertyName("installedConflictableMods"), Obsolete]
    public Dictionary<int, List<int>> InstalledConflictableMods { get; set; } = [];

    /// <summary>All installed mods, keyed by an ID (can be the Gamebanana submission ID or a random value for a local mod).</summary>
    [JsonPropertyName("modsV2")]
    public Dictionary<UInt128, InstalledMod2> Mods2 { get; set; } = [];

    /// <summary>Installed mods which are prone to conflicts, like skins and some model/texture replacements (key: category ID, value: mod IDs).</summary>
    [JsonPropertyName("installedConflictableModsV2")]
    public Dictionary<int, List<UInt128>> InstalledConflictableMods2 { get; set; } = [];

    internal void Migrate(int lastUpdatedBy)
    {
        if (lastUpdatedBy != 0)
            return;

#pragma warning disable CS0612 // Type or member is obsolete

        InstalledConflictableMods2 = new(InstalledConflictableMods
            .Select(static kvp => new KeyValuePair<int, List<UInt128>>(kvp.Key, kvp.Value.ConvertAll(i => (UInt128)i))));

        Mods2 = new(Mods.Select(static kvp =>
        {
            Legacy.InstalledMod old = kvp.Value;
            Legacy.GamebananaMetadata? submission = old.GamebananaMetadata;

            return new KeyValuePair<UInt128, InstalledMod2>((UInt128)kvp.Key, new InstalledMod2
            {
                Name = old.ListingMetadata?.Name ?? "[no name provided]",
                Author = old.ListingMetadata?.Author,
                GamebananaSubmission = submission is not null
                    ? new GamebananaSubmission
                    {
                        Id = submission.SubmissionId,
                        Type = submission.SubmissionType,
                        CategoryType = submission.SubmissionCategoryType,
                        CategoryBranch = submission.SubmissionCategoryTree,
                    } : null,
                Files = [new InstalledModFile
                {
                    RelativePath = old.RelativePath,
                    Md5Checksum = old.OriginalFileMd5Checksum,
                    GamebananaFile = submission is not null
                        ? new GamebananaFile
                        {
                            Id = submission.FileId,
                            Name = submission.FileName,
                            UploadDate = submission.FileUploadDate,
                            ArchiveMd5Checksum = submission.FileMd5Checksum
                        } : null,
                }]
            });
        }));

#pragma warning restore CS0612 // Type or member is obsolete
    }
}

/// <summary>Entry for an installed mod.</summary>
public sealed class InstalledMod2
{
    /// <summary>The user-readable name of the mod.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>The user-readable name of the mod's author.</summary>
    [JsonPropertyName("author")]
    public required string? Author { get; set; }

    /// <summary>Gamebanana submission metadata. If <see langword="null"/>, this is a local mod.</summary>
    [JsonPropertyName("gamebananaSubmission")]
    public required GamebananaSubmission? GamebananaSubmission { get; set; }

    /// <summary>The files associated with this mod.</summary>
    [JsonPropertyName("files")]
    public required List<InstalledModFile> Files { get; init; }
}

public sealed class InstalledModFile
{
    /// <summary>Relative path to the vpk file from the 'Deadlock\game\citadel\addons' directory.</summary>
    [JsonPropertyName("relativePath")]
    public required string RelativePath { get; set; }

    /// <summary>The original MD5 file checksum.</summary>
    [JsonPropertyName("md5Checksum")]
    public required string Md5Checksum { get; set; }

    /// <summary>Gamebanana file metadata. If <see langword="null"/>, this is a local mod.</summary>
    [JsonPropertyName("gamebananaFile")]
    public required GamebananaFile? GamebananaFile { get; set; }
}

public sealed class GamebananaSubmission
{
    /// <summary>The Gamebanana submission ID.</summary>
    [JsonPropertyName("id")]
    public required int Id { get; set; }

    /// <summary>The Gamebanana submission type (Mod/Sound).</summary>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <summary>The Gamebanana submission category.</summary>
    [JsonPropertyName("categoryType")]
    public required string? CategoryType { get; set; }

    /// <summary>The full Gamebanana submission category (ID) branch.</summary>
    [JsonPropertyName("categoryBranch")]
    public required List<int> CategoryBranch { get; set; }
}

public sealed class GamebananaFile
{
    /// <summary>The Gamebanana file ID.</summary>
    [JsonPropertyName("id")]
    public required int Id { get; set; }

    /// <summary>The original file name from Gamebanana.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>The date the file was uploaded to Gamebanana.</summary>
    [JsonPropertyName("uploadDate")]
    public required DateTime UploadDate { get; set; }

    /// <summary>The original MD5 archive checksum.</summary>
    [JsonPropertyName("archiveMd5Checksum")]
    public required string ArchiveMd5Checksum { get; set; }
}
