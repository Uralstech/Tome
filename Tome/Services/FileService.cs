using System.IO;
using System.Text.Json;
using Tome.Models;

namespace Tome.Services;

public sealed class FileService
{
    private static string AppDataDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Uralstech", "Tome");

    public string GetSaveDirectory()
    {
        string path = AppDataDirectory;
        Directory.CreateDirectory(path);
        return path;
    }

    public void WriteMetaFor(string filePath)
    {
        string meta = JsonSerializer.Serialize(new TomeMeta
        {
            BackupBy = VersionInfo.BuildId,
        }, TomeMetaJsonContext.Default.TomeMeta);

        string metaFile = $"{filePath}.meta";
        File.WriteAllText(metaFile, meta);
    }
}
