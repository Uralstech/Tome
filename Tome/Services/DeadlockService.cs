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

using Microsoft.Win32;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using Tome.Models;
using ValveKeyValue;

namespace Tome.Services;

public sealed class DeadlockService(FileService fileService)
{
    private const string DeadlockAppId = "1422450";
    private const string GameInfoBackup = "gameinfo.gi.backup";

    private static readonly KVSerializer s_kv1Serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

    private readonly FileService _fileService = fileService;

    public Process? LaunchDeadlock()
    {
        return Process.Start(new ProcessStartInfo
        {
            FileName = $"steam://rungameid/{DeadlockAppId}",
            UseShellExecute = true
        });
    }

    public string GetAddonsPath(string basePath)
    {
        string directory = Path.Combine(basePath, "game\\citadel\\addons");
        Directory.CreateDirectory(directory);
        return directory;
    }

    #region Manifest Verification

    public bool VerifyManifest(TomeManifest manifest, [NotNullWhen(false)] out ManifestVerificationFailures? failures)
    {
        IEnumerable<InstalledMod> allMods = manifest.Skins.Values.Concat(
            manifest.ModelReplacements.Values
        ).Append(manifest.Misc).SelectMany(static mod => mod);

        string safeAddonsPath = GetAddonsPath(manifest.GamePath);
        if (!Path.EndsInDirectorySeparator(safeAddonsPath))
            safeAddonsPath += Path.DirectorySeparatorChar;

        Uri baseUri = new(safeAddonsPath);

        HashSet<string> allModPathsRelative = [];
        Dictionary<string, string> problematicMods = [];
        foreach (InstalledMod mod in allMods)
        {
            allModPathsRelative.Add(mod.RelativePath);

            string absoluteModPath = Path.GetFullPath(Path.Combine(safeAddonsPath, mod.RelativePath));
            if (!baseUri.IsBaseOf(new Uri(absoluteModPath)))
            {
                problematicMods.TryAdd(mod.RelativePath, $"'{mod.ListingMetadata?.Name ?? mod.RelativePath}' is outside {safeAddonsPath}");
                continue;
            }

            if (!File.Exists(absoluteModPath))
            {
                problematicMods.TryAdd(mod.RelativePath, $"'{mod.ListingMetadata?.Name ?? ""}' is missing at path: {absoluteModPath}");
                continue;
            }

            if (CryptographyUtil.Md5File(absoluteModPath) != mod.OriginalFileMd5Checksum)
            {
                problematicMods.TryAdd(mod.RelativePath, $"'{mod.ListingMetadata?.Name ?? mod.RelativePath}' does not match original checksum");
                continue;
            }
        }

        foreach (string absolutePath in Directory.EnumerateFiles(safeAddonsPath))
        {
            string relativePath = Path.GetRelativePath(safeAddonsPath, absolutePath);
            if (!allModPathsRelative.Contains(relativePath))
                problematicMods.TryAdd(relativePath, $"Unknown/unregistered mod at path: {absolutePath}");
        }

        if (problematicMods.Count > 0)
        {
            failures = new ManifestVerificationFailures()
            {
                ProblematicMods = problematicMods
            };

            return false;
        }

        failures = null;
        return true;
    }

    public void ClearManifestFailures(TomeManifest manifest, ManifestVerificationFailures failures, string backupZip)
    {
        string safeAddonsPath = GetAddonsPath(manifest.GamePath);
        if (!Path.EndsInDirectorySeparator(safeAddonsPath))
            safeAddonsPath += Path.DirectorySeparatorChar;

        Uri baseUri = new(safeAddonsPath);
        HashSet<string> filesToDelete = [];

        using (FileStream zipToOpen = new(backupZip, FileMode.Create))
        using (ZipArchive archive = new(zipToOpen, ZipArchiveMode.Create))
        {
            HashSet<string> problematicModPathsRelative = [.. failures.ProblematicMods.Keys];
            foreach (string problematicModPathRelative in problematicModPathsRelative)
            {
                string absoluteModPath = Path.GetFullPath(Path.Combine(safeAddonsPath, problematicModPathRelative));
                if (!File.Exists(absoluteModPath))
                    continue;

                string modFileName = Path.GetFileName(absoluteModPath);
                string modPathInZip = Path.Combine("addons", modFileName);
                archive.CreateEntryFromFile(absoluteModPath, modPathInZip);

                if (baseUri.IsBaseOf(new Uri(absoluteModPath)))
                    filesToDelete.Add(absoluteModPath);
            }

            IEnumerable<List<InstalledMod>> allModsLists = manifest.Skins.Values.Concat(
                manifest.ModelReplacements.Values
            ).Append(manifest.Misc);

            foreach (List<InstalledMod> modsList in allModsLists)
                modsList.RemoveAll(mod => problematicModPathsRelative.Contains(mod.RelativePath));
        }

        foreach (string fileToDelete in filesToDelete)
            File.Delete(fileToDelete);

        _fileService.SaveManifest(manifest);
    }

    #endregion

    #region GameInfo Management

    public string GetGameInfoPath(string basePath) =>
        Path.Combine(basePath, "game\\citadel\\gameinfo.gi");

    public void RestoreGameInfo(string path)
    {
        string file = GetGameInfoPath(path);
        string saveDirectory = _fileService.GetSaveDirectory();
        string backupPath = Path.Combine(saveDirectory, CryptographyUtil.Md5(path.TrimEnd('/', '\\')), GameInfoBackup);

        File.Copy(backupPath, file, true);
    }

    public void PatchDeadlockGameInfo(string path, out bool corruptWithBackup)
    {
        corruptWithBackup = false;
        string file = GetGameInfoPath(path);

        string saveDirectory = _fileService.GetSaveDirectory();
        string backupPath = Path.Combine(saveDirectory, CryptographyUtil.Md5(path.TrimEnd('/', '\\')), GameInfoBackup);
        bool isFreshBackup = false;

        if (!File.Exists(backupPath))
        {
            string backupDir = Path.GetDirectoryName(backupPath)!;
            Directory.CreateDirectory(backupDir);
            
            _fileService.WriteMetaFor(backupPath);
            File.Copy(file, backupPath);
            isFreshBackup = true;
        }

        try
        {
            KVDocument document;
            using (FileStream input = File.OpenRead(file))
                document = s_kv1Serializer.Deserialize(input);

            KeyValuePair<string, KVObject>[] searchPaths = [.. document["FileSystem"]["SearchPaths"]];
            int searchPathCount = searchPaths.Length;

            const string ExceptionMessage = "citadel\\gameinfo.gi is an unexpected format and may be corrupt. " +
                                             "Please verify Deadlock's integrity through Steam. If that does not " +
                                             "fix this warning, you may have to update Tome.";

            bool isValid = searchPathCount > 0;

            KVObject searchPathsCopy = KVObject.ListCollection(searchPathCount);
            Dictionary<string, int> citadelEntries = new(3);
            Dictionary<string, int> coreEntries = new(3);
            int addonsIndex = -1;

            for (int i = 0; i < searchPathCount; i++)
            {
                (string key, KVObject value) = searchPaths[i];
                string valueStr = value.ToString();

                if (valueStr is "citadel/addons")
                {
                    if (addonsIndex is >= 0 || key is not "Game"
                        || citadelEntries.Count > 0 || coreEntries.Count > 0)
                    {
                        isValid = false;
                        break;
                    }

                    addonsIndex = i;
                    continue;
                }

                if (valueStr is "citadel")
                {
                    if (key is "Game" or "Mod" or "Write"
                        && citadelEntries.TryAdd(key, i)
                        && citadelEntries.Count <= 3)
                        continue;

                    isValid = false;
                    break;
                }

                if (valueStr is "core")
                {
                    if (key is "Game" or "Mod" or "Write"
                        && coreEntries.TryAdd(key, i)
                        && coreEntries.Count <= 3)
                        continue;

                    isValid = false;
                    break;
                }

                searchPathsCopy.Add(key, value);
            }

            if (!isValid)
            {
                if (isFreshBackup)
                    throw new FormatException(ExceptionMessage);

                corruptWithBackup = true;
                return;
            }

            searchPathsCopy.Add("Game", "citadel/addons");
            searchPathsCopy.Add("Mod", "citadel");
            searchPathsCopy.Add("Write", "citadel");
            searchPathsCopy.Add("Game", "citadel");
            searchPathsCopy.Add("Mod", "core");
            searchPathsCopy.Add("Write", "core");
            searchPathsCopy.Add("Game", "core");

            if (citadelEntries.Count == 3
                && coreEntries.Count == 3
                && addonsIndex >= 0)
            {
                Debug.WriteLine("gameinfo.gi is already patched.");
                return;
            }

            document["FileSystem"]["SearchPaths"] = searchPathsCopy;
            using (FileStream output = File.Create(file))
                s_kv1Serializer.Serialize(output, document);

            Debug.WriteLine("gameinfo.gi successfully patched.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception while getting Deadlock path: {ex}");
            if (isFreshBackup)
                throw;

            corruptWithBackup = true;
        }
    }

    #endregion

    #region Deadlock Install Management

    public bool TryGetDeadlockPath([NotNullWhen(true)] out string? path)
    {
        path = null;

        try
        {
            // https://github.com/WilliamVenner/steamlocate-rs/blob/master/src/locate/windows.rs
            string? steamPath = (Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Valve\\Steam", "InstallPath", null)
                                 ?? Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Valve\\Steam", "InstallPath", null)
                                 ?? Registry.GetValue("HKEY_CURRENT_USER\\Software\\Valve\\Steam", "SteamPath", null)) as string;

            if (!Directory.Exists(steamPath))
                return false;

            string libraryPathsFile = Path.Combine(steamPath, "steamapps\\libraryfolders.vdf");
            if (!File.Exists(libraryPathsFile))
                return false;

            KVDocument document;
            using (FileStream input = File.OpenRead(libraryPathsFile))
                document = s_kv1Serializer.Deserialize(input);

            if (document.Name != "libraryfolders")
                return false;

            foreach ((string _, KVObject library) in document.Root.Children)
            {
                if (!library.TryGetValue("apps", out KVObject? apps)
                    || !library.TryGetValue("path", out KVObject? libraryPath))
                    continue;

                foreach ((string appId, KVObject _) in apps)
                {
                    if (appId != DeadlockAppId)
                        continue;

                    string libraryPathStr = libraryPath.ToString();
                    if (!Directory.Exists(libraryPathStr))
                        continue;

                    string deadlockManifest = Path.Combine(libraryPathStr, $"steamapps\\appmanifest_{DeadlockAppId}.acf");
                    if (!File.Exists(deadlockManifest))
                        continue;

                    string gamePath = Path.Combine(libraryPathStr, "steamapps\\common\\Deadlock");
                    if (!Directory.Exists(gamePath))
                        continue;

                    path = Path.GetFullPath(gamePath);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception while getting Deadlock path: {ex}");
        }

        return false;
    }

    public bool VerifyDeadlockInstallation([NotNullWhen(true)] string? path)
    {
        try
        {
            if (!Directory.Exists(path))
                return false;

            string file = GetGameInfoPath(path);
            if (!File.Exists(file))
                return false;

            KVDocument document;
            using (FileStream input = File.OpenRead(file))
                document = s_kv1Serializer.Deserialize(input);

            return document.Name == "GameInfo"
                && document.Root.TryGetValue("game", out KVObject? game)
                && game.ToString() == "citadel";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception while verifying Deadlock: {ex}");
            return false;
        }
    }

    #endregion
}
