using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using Tome.Models;

namespace Tome.Services;

public sealed class FileService
{
    private static string GetAppDataDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Uralstech", "Tome");

    private static string GetIinstallationsManifestPath() =>
        Path.Combine(GetAppDataDirectory(), "tomeManifest.json");

    public string GetSaveDirectory()
    {
        string path = GetAppDataDirectory();
        Directory.CreateDirectory(path);
        return path;
    }

    public bool TryGetManifest([NotNullWhen(true)] out TomeManifest? manifest)
    {
        manifest = null;
        
        try
        {
            string path = GetIinstallationsManifestPath();
            if (!File.Exists(path))
                return false;

            using FileStream stream = File.OpenRead(path);
            
            manifest = JsonSerializer.Deserialize(stream, TomeManifestJsonContext.Default.TomeManifest);
            return manifest != null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get manifest: {ex}");
            return false;
        }
    }

    public void SaveManifest(TomeManifest manifest)
    {
        string path = GetIinstallationsManifestPath();
        string dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);

        WriteMetaFor(path);

        using FileStream stream = File.Create(path);
        JsonSerializer.Serialize(stream, manifest, TomeManifestJsonContext.Default.TomeManifest);
    }

    public void WriteMetaFor(string filePath)
    {
        string meta = JsonSerializer.Serialize(new TomeMeta
        {
            LastUpdatedBy = VersionInfo.BuildId,
        }, TomeMetaJsonContext.Default.TomeMeta);

        string metaFile = $"{filePath}.meta";
        File.WriteAllText(metaFile, meta);
    }
}
