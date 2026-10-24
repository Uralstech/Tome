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

    public string GetGameInfoPath(string basePath) =>
        Path.Combine(basePath, "game\\citadel\\gameinfo.gi");

    public void ClearAllMods(InstallationsManifest manifest, string backupZip)
    {
        string addons = GetAddonsPath(manifest.GamePath);

        if (Directory.EnumerateFileSystemEntries(addons).Any())
            ZipFile.CreateFromDirectory(addons, backupZip);

        manifest.Skins.Clear();
        manifest.ModelReplacements.Clear();
        manifest.Misc.Clear();

        _fileService.SaveManifest(manifest);
        Directory.Delete(addons, true);
    }

    public bool VerifyManifest(InstallationsManifest manifest, out IReadOnlyList<string> warnings)
    {
        IEnumerable<InstalledMod> allMods = manifest.Skins.Values.Concat(
            manifest.ModelReplacements.Values
        ).Append(manifest.Misc).SelectMany(static mod => mod);

        List<string> warningsList = [];
        warnings = warningsList;

        string safeAddonsPath = GetAddonsPath(manifest.GamePath);
        if (!Path.EndsInDirectorySeparator(safeAddonsPath))
            safeAddonsPath += Path.DirectorySeparatorChar;

        Uri baseUri = new(safeAddonsPath);

        foreach (InstalledMod mod in allMods)
        {
            if (!baseUri.IsBaseOf(new Uri(mod.Path)))
            {
                warningsList.Add($"Mod '{mod.ListingMetadata?.Name ?? ""}' is located at an unexpected directory relative to {safeAddonsPath}");
                continue;
            }

            if (!File.Exists(mod.Path))
            {
                warningsList.Add($"Mod '{mod.ListingMetadata?.Name ?? ""}' missing at path: {mod.Path}");
                continue;
            }

            if (CryptographyUtil.Md5File(mod.Path) != mod.OriginalFileMd5Checksum)
            {
                warningsList.Add($"Mod '{mod.ListingMetadata?.Name ?? ""}' does not match original checksum and may be corrupt");
                continue;
            }
        }

        return warningsList.Count == 0;
    }

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

    public bool TryGetDeadlockPath([NotNullWhen(true)] out string? path)
    {
        path = null;

        try
        {
            // https://github.com/WilliamVenner/steamlocate-rs/blob/master/src/locate/windows.rs
            string? steamPath = (Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Valve\\Steam", "InstallPath", null)
                                 ?? Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Valve\\Steam", "InstallPath", null)
                                 ?? Registry.GetValue("HKEY_CURRENT_USER\\Software\\Valve\\Steam", "InstallPath", null)) as string;

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

                    path = gamePath;
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
}
