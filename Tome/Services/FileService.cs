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

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using Tome.Models;

namespace Tome.Services;

public sealed class FileService
{
    private static string GetAppDataDirectory()
    {
        string localAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);

        string path = Path.Combine(localAppData, "Uralstech", "Tome");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string GetInstallationsManifestPath() =>
        Path.Combine(GetAppDataDirectory(), "tomeManifest.json");

    public string GetSaveDirectory() => GetAppDataDirectory();

    public bool TryGetManifest([NotNullWhen(true)] out TomeManifest? manifest)
    {
        manifest = null;
        
        try
        {
            string path = GetInstallationsManifestPath();
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
        string path = GetInstallationsManifestPath();
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
