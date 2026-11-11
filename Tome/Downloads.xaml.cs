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

using GamebananaAPI.Data.Categories;
using GamebananaAPI.Data.Submission;
using Microsoft.Extensions.DependencyInjection;
using SharpCompress.Archives;
using SharpCompress.Common;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Tome.Data;
using Tome.Models;
using Tome.Services;

namespace Tome;

public partial class Downloads : Window
{
    [GeneratedRegex(@"\Apak\d\d_dir\.vpk\z", RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex VpkMatchRegexGen();

    private static readonly Regex s_vpkMatchRegex = VpkMatchRegexGen();
    private static readonly Uri s_invalidUri = new("tome:invalid");

    private const int SkinSuperCategory = 33295;
    private static readonly HashSet<int> s_conflictableChildCategories = [
        49852, // Rat King Flag
        44206, // Hideout
        44458, // Walker
        44686, // Midboss
        44687, // Troopers
        44688, // Guardian
        46849, // Sandbox dummy
        45979, // Rejuv
        44391, // Sinners
        41868, // Soul bag
        41656, // Urn
    ];

    private readonly FileService _files;
    private readonly DeadlockService _deadlock;
    private readonly GamebananaService _gamebanana;

    private readonly TomeManifest _manifest;
    private readonly ModActions _requestedActions;

    private readonly TaskCompletionSource<bool> _confirmTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private TaskCompletionSource? _fileSelectionTcs = null;

    public Downloads(ModActions actions, TomeManifest manifest)
    {
        InitializeComponent();

        _manifest = manifest;
        _requestedActions = actions;
        _files = App.Services.GetRequiredService<FileService>();
        _deadlock = App.Services.GetRequiredService<DeadlockService>();
        _gamebanana = App.Services.GetRequiredService<GamebananaService>();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        FileSelectionBorder.Visibility = Visibility.Collapsed;
        DownloadProgressBorder.Visibility = Visibility.Collapsed;
        CancelContinueButtons.Visibility = Visibility.Collapsed;
    }

    private async void Window_ContentRendered(object sender, EventArgs e)
    {
        Queue<(ModAction, List<ModFile>)>? actionsQueue = await LoadSubmissionsAsync();
        if (actionsQueue is null)
        {
            Close();
            return;
        }
        
        DialogResult = await ExecuteActionsAsync(actionsQueue);
        RebuildConflictableCategories();
        _files.SaveManifest(_manifest);
        Close();
    }

    #region File Selection
    
    private async Task<Queue<(ModAction, List<ModFile>)>?> LoadSubmissionsAsync()
    {
        try
        {
            CancelContinueButtons.IsEnabled = false;
            CancelContinueButtons.Visibility = Visibility.Visible;

            Log("Loading submission details, please wait...");

            StringBuilder sb = new();
            sb.AppendLine("The following actions have been requested:");

            Queue<(ModAction, List<ModFile>)> queue = [];
            string addonsFolder = _deadlock.GetAddonsPath(_manifest.GamePath);

            foreach ((UInt128 id, ModAction action) in _requestedActions)
            {
                _manifest.Mods2.TryGetValue(id, out InstalledMod2? installedMod);
                List<ModFile> files = [];
                
                FileInfo fileInfo;
                switch (action.Type)
                {
                    case ModActionType.Update:
                    case ModActionType.Install:
                        if (action.Mod.IsLocal)
                        {
                            if (action.Mod is not OfflineListedMod offlineMod)
                                throw new InvalidOperationException("Tried using online mod which was declared local.");

                            fileInfo = new FileInfo(offlineMod.Path);
                            files.Add(new ModFile(
                                Source: new Uri(fileInfo.FullName, UriKind.Absolute),
                                Name: fileInfo.Name,
                                Md5Hash: CryptographyUtil.Md5File(fileInfo.FullName),
                                Size: fileInfo.Length,

                                TargetPath: installedMod?.Files is { Count: > 0 }
                                    ? Path.Combine(addonsFolder, installedMod.Files[0].RelativePath) : null,
                                GamebananaFile: null
                            ));

                            if (installedMod?.Files is { Count: > 1 })
                            {
                                files.AddRange(installedMod.Files[1..].Select(missedFile => new ModFile(
                                    Source: s_invalidUri,
                                    Name: missedFile.RelativePath,
                                    Md5Hash: missedFile.Md5Checksum,
                                    Size: 0,

                                    TargetPath: Path.Combine(addonsFolder, missedFile.RelativePath),
                                    GamebananaFile: null
                                )));
                            }

                            break;
                        }

                        if (action.Mod is not OnlineListedMod onlineMod)
                            throw new InvalidOperationException("Tried using local mod which was declared online.");

                        SubmissionProfile submission = await _gamebanana.GetSubmissionAsync(onlineMod.SubmissionId, onlineMod.SubmissionType);
                        if (submission.Files is not { Count: > 0 })
                        {
                            MessageBoxResult result = MessageBox.Show(this, $"'{submission.Name}' does not have any downloadable file. Skip?", "Invalid mod",
                                                                            MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                            if (result == MessageBoxResult.OK)
                                continue;

                            return null;
                        }

                        if (action.Type is ModActionType.Install)
                        {
                            if (submission.Files.Count == 1)
                            {
                                SubmissionFile singleFile = submission.Files[0];
                                files.Add(new ModFile(
                                    Source: new Uri(singleFile.DownloadUrl),
                                    Name: singleFile.Name,
                                    Md5Hash: singleFile.Md5Checksum,
                                    Size: singleFile.Size,

                                    TargetPath: null,
                                    GamebananaFile: singleFile
                                ));
                                break;
                            }

                            IEnumerable<SubmissionFile> selectedFiles = await AskUserForFilesAsync(submission);
                            files.AddRange(selectedFiles.Select(onlineFile =>
                                new ModFile(
                                    Source: new Uri(onlineFile.DownloadUrl),
                                    Name: onlineFile.Name,
                                    Md5Hash: onlineFile.Md5Checksum,
                                    Size: onlineFile.Size,

                                    TargetPath: null,
                                    GamebananaFile: onlineFile
                                )));

                            break;
                        }

                        Dictionary<string, InstalledModFile> installedFiles = new(installedMod!.Files
                            .Select(static installedFile => new KeyValuePair<string, InstalledModFile>(installedFile.GamebananaFile!.Name, installedFile)));

                        Dictionary<string, SubmissionFile> availableFiles = new(submission.Files
                            .Select(static onlineFile => new KeyValuePair<string, SubmissionFile>(onlineFile.Name, onlineFile)));

                        List<(InstalledModFile Old, SubmissionFile New)> pairs = [.. installedFiles.Keys.Intersect(availableFiles.Keys)
                            .Select(key =>
                            {
                                installedFiles.Remove(key, out InstalledModFile? installedFile);
                                availableFiles.Remove(key, out SubmissionFile? onlineFile);
                                return (installedFile!, onlineFile!);
                            })];

                        List<InstalledModFile> missingUpdates = [];
                        foreach ((string name, InstalledModFile installedFile) in installedFiles)
                        {
                            SubmissionFile? onlineFile = await AskUserForMissingUpdateAsync(submission, name, availableFiles.Values);
                            if (onlineFile is null)
                            {
                                missingUpdates.Add(installedFile);
                                continue;
                            }

                            pairs.Add((installedFile, onlineFile));
                            availableFiles.Remove(onlineFile.Name);
                        }

                        if (missingUpdates.Count > 0
                            && (availableFiles.Count > 0
                                || MessageBox.Show(this, $"The following files from '{submission.Name}' are no longer listed, and there are no alternatives. " +
                                                         $"They will be deleted if you choose to continue with the update. Continue?",
                                                         "Files unlisted", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes))
                                continue;

                        files.AddRange(pairs.Select(pair => new ModFile(
                            Source: new Uri(pair.New.DownloadUrl),
                            Name: pair.New.Name,
                            Md5Hash: pair.New.Md5Checksum,
                            Size: pair.New.Size,

                            TargetPath: Path.Combine(addonsFolder, pair.Old.RelativePath),
                            GamebananaFile: pair.New
                        )));

                        files.AddRange(missingUpdates.Select(missedFile => new ModFile(
                            Source: s_invalidUri,
                            Name: missedFile.RelativePath,
                            Md5Hash: missedFile.Md5Checksum,
                            Size: 0,

                            TargetPath: Path.Combine(addonsFolder, missedFile.RelativePath),
                            GamebananaFile: null
                        )));

                        break;

                    case ModActionType.Delete:
                        foreach (InstalledModFile installedFile in installedMod!.Files)
                        {
                            string fullPath = Path.Combine(addonsFolder, installedFile.RelativePath);
                            fileInfo = new FileInfo(fullPath);

                            files.Add(new ModFile(
                                Source: new Uri(fileInfo.FullName, UriKind.Absolute),
                                Name: fileInfo.Name,
                                Md5Hash: installedFile.Md5Checksum,
                                Size: fileInfo.Length,

                                TargetPath: fileInfo.FullName,
                                GamebananaFile: null
                            ));
                        }

                        break;

                    default:
                        throw new NotImplementedException($"Case for '{action.Type}' not implemented.");
                }

                sb.AppendLine($"- {action.Type} '{action.Mod.Name}'");
                queue.Enqueue((action, files));
            }

            Log(sb.ToString());
            CancelContinueButtons.IsEnabled = true;

            if (!await _confirmTcs.Task)
            {
                MessageBox.Show(this, "The requested actions were cancelled.", "Actions cancelled",
                                      MessageBoxButton.OK, MessageBoxImage.Information);

                return null;
            }

            CancelContinueButtons.Visibility = Visibility.Hidden;
            return queue;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not load submissions",
                                  MessageBoxButton.OK, MessageBoxImage.Error);

            return null;
        }
    }

    private async Task<SubmissionFile?> AskUserForMissingUpdateAsync(SubmissionProfile submission, string missing, IEnumerable<SubmissionFile> available)
    {
        const string GroupName = nameof(AskUserForMissingUpdateAsync);

        string message = $"'{submission.Name}' does not host the originally installed file '{missing}' anymore. Choose an alternative, or skip updating:";
        List<OnlineModFileModel> options = [.. available.Select(static file =>
            new OnlineModFileModel(!string.IsNullOrEmpty(file.Description)
                                        ? $"{file.Name}: {file.Description}" : file.Name, GroupName, file)
            ).Append(
            new OnlineModFileModel("Skip update", GroupName, null))
        ];
    
        return (await BuiltInFileDialogAsync(message, options)).First();
    }

    private async Task<IEnumerable<SubmissionFile>> AskUserForFilesAsync(SubmissionProfile submission)
    {
        string message = $"'{submission.Name}' has multiple files. Choose one or more to install:";
        List<OnlineModFileModel> options = [.. submission.Files!.Select(static file =>
            new OnlineModFileModel(!string.IsNullOrEmpty(file.Description)
                                        ? $"{file.Name}: {file.Description}" : file.Name, null, file)
            )
        ];

        return (IEnumerable<SubmissionFile>)await BuiltInFileDialogAsync(message, options);
    }
    
    private async Task<IEnumerable<SubmissionFile?>> BuiltInFileDialogAsync(string message, List<OnlineModFileModel> options)
    {
        FileSelectionText.Text = message;
        _fileSelectionTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    
        options[0].IsSelected = true;
        FileSelectionButtons.ItemsSource = options;
    
        FileSelectionBorder.Visibility = Visibility.Visible;
        await _fileSelectionTcs.Task;
    
        List<SubmissionFile?> results = [.. options
            .Where(static model => model.IsSelected)
            .Select(static model => model.File)];
        
        FileSelectionBorder.Visibility = Visibility.Collapsed;
        if (results.Count > 0)
            return results;

        MessageBox.Show(this, "Please select at least one option to continue.", "Invalid selection",
                              MessageBoxButton.OK, MessageBoxImage.Error);

        FileSelectionButtons.ItemsSource = null;
        return await BuiltInFileDialogAsync(message, options);
    }
    
    private void FileSelection_Click(object sender, RoutedEventArgs e) =>
        _fileSelectionTcs?.TrySetResult();

    // https://stackoverflow.com/a/40041747
    private void FileSelectionRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radioButton
            && string.IsNullOrEmpty(radioButton.GroupName))
            radioButton.Tag = true;
    }

    private void FileSelectionRadioButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton radioButton
            || !string.IsNullOrEmpty(radioButton.GroupName))
            return;

        if (radioButton.Tag is null or true)
        {
            radioButton.Tag = false;
            return;
        }

        if (radioButton.IsChecked == true)
            radioButton.IsChecked = false;
    }

    #endregion

    private async Task<bool> ExecuteActionsAsync(Queue<(ModAction, List<ModFile>)> queue)
    {
        List<string> tempFiles = [];
        try
        {
            DownloadProgressBorder.Visibility = Visibility.Visible;
            DownloadProgressBar.Minimum = 0;
            DownloadProgressBar.Maximum = 1;

            foreach ((ModAction action, List<ModFile> files) in queue)
            {
                SubmissionProfile? submission = action.Mod is OnlineListedMod onlineMod
                    ? await _gamebanana.GetSubmissionAsync(onlineMod.SubmissionId, onlineMod.SubmissionType)
                    : null;

                if (action.Type is ModActionType.Install
                    && action.Mod is OnlineListedMod { SubmissionType: "Mod" }
                    && submission?.Category is Category category
                    && FindConflictableCategory(await GetCategoryTreeAsync(category), out int conflictableCategory)
                    && _manifest.InstalledConflictableMods2.TryGetValue(conflictableCategory, out List<UInt128>? conflicts))
                {
                    string[] conflictingModNames = [.. conflicts
                        .Where(modId => !_requestedActions.TryGetValue(modId, out ModAction? action) || action.Type is not ModActionType.Delete)
                        .Select(modId => _manifest.Mods2[modId].Name)];

                    if (conflictingModNames.Length > 0
                        && MessageBox.Show(this, $"'{action.Mod.Name}' may conflict with:\n- {string.Join("\n- ", conflictingModNames)}\n\n" +
                                                 $"Continue? Please check the author's notes on compatability with other mods.",
                                                 "Invalid mod", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                        continue;
                }

                switch (action.Type)
                {
                    case ModActionType.Update:
                    case ModActionType.Install:
                        ModFile[] deletes = [.. files.Where(static file => file.Source == s_invalidUri)];
                        if (!VerifyDeletions(action, deletes))
                            continue;

                        List<(string, ModFile)> paths = [];
                        foreach (ModFile file in files)
                        {
                            if (file.Source == s_invalidUri)
                                continue;

                            if (file.Source.Scheme is "file")
                            {
                                paths.Add((file.Source.LocalPath, file));
                                continue;
                            }

                            CurrentDownloadText.Text = $"Downloading {action.Mod.Name} ('{file.Name}')...";
                            DownloadProgressBar.Value = 0;

                            string tempPath = Path.GetTempFileName();
                            tempFiles.Add(tempPath);

                            await _gamebanana.DownloadAsync(file.Source, tempPath, progress =>
                                DownloadProgressBar.Value = (double)progress / file.Size);
                            
                            paths.Add((tempPath, file));
                        }
                
                        CurrentDownloadText.Text = $"Processing {action.Mod.Name}...";
                        DownloadProgressBar.IsIndeterminate = true;

                        List<InstalledModFile>? results = await ProcessDownloadedContentAsync(paths, action);
                        if (results is null)
                            return false;
                
                        DownloadProgressBar.IsIndeterminate = false;
                        if (results.Count == 0)
                            continue;

                        _manifest.Mods2[action.Mod.Id] = new InstalledMod2
                        {
                            Name = action.Mod.Name,
                            Author = action.Mod.Author,
                            GamebananaSubmission = submission is not null
                                ? new GamebananaSubmission
                                {
                                    Id = submission.Id,
                                    Type = ((OnlineListedMod)action.Mod).SubmissionType,
                                    CategoryType = submission.Category?.Type,
                                    CategoryBranch = await GetCategoryTreeAsync(submission.Category)
                                } : null,
                            Files = results,
                        };

                        if (deletes.Length > 0)
                        {
                            CurrentDownloadText.Text = $"Deleting non-updatable files from {action.Mod.Name}...";
                            DownloadProgressBar.Value = 1;

                            foreach (ModFile file in deletes)
                                File.Delete(file.TargetPath!);
                        }

                        break;
                
                    case ModActionType.Delete:
                        CurrentDownloadText.Text = $"Deleting {action.Mod.Name}...";
                        DownloadProgressBar.Value = 1;

                        if (!VerifyDeletions(action, files))
                            continue;
                
                        _manifest.Mods2.Remove(action.Mod.Id);
                
                        foreach (ModFile file in files)
                            File.Delete(file.TargetPath!);
                        break;
                
                    default:
                        throw new NotImplementedException($"Case for '{action.Type}' not implemented.");
                }

                RebuildConflictableCategories();
                _files.SaveManifest(_manifest);
            }

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not complete all actions",
                                  MessageBoxButton.OK, MessageBoxImage.Error);

            return false;
        }
        finally
        {
            foreach (string tempFile in tempFiles)
                File.Delete(tempFile);
        }
    }

    private bool VerifyDeletions(ModAction action, IEnumerable<ModFile> files)
    {
        string addonsPath = _deadlock.GetAddonsPath(_manifest.GamePath);
        string[] paths = [.. files
                            .Where(file => !CryptographyUtil.Md5File(file.TargetPath!).Equals(file.Md5Hash, StringComparison.InvariantCulture))
                            .Select(file => Path.GetRelativePath(addonsPath, file.TargetPath!))];

        return paths.Length == 0
            || MessageBox.Show(this, $"'{action.Mod.Name}' does not match its known checksums for:\n- {string.Join("\n- ", paths)}\n\n" +
                                     $"You may have edited the mod outside of Tome. Continue with deletion?", "Checksum mismatch",
                                     MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    private async Task<List<InstalledModFile>?> ProcessDownloadedContentAsync(List<(string, ModFile)> paths, ModAction action)
    {
        string addonsFolder = _deadlock.GetAddonsPath(_manifest.GamePath);
        List<(string, ModFile)> validatedPaths = [];

        foreach ((string path, ModFile file) in paths)
        {
            if (action.Type is ModActionType.Update && !string.IsNullOrEmpty(file.TargetPath))
            {
                string existingMd5 = CryptographyUtil.Md5File(file.TargetPath);
                InstalledModFile existingFile = _manifest.Mods2[action.Mod.Id].Files.First(installedFile =>
                    installedFile.RelativePath == Path.GetRelativePath(addonsFolder, file.TargetPath));

                if (!existingMd5.Equals(existingFile.Md5Checksum, StringComparison.InvariantCulture))
                {
                    MessageBoxResult result = MessageBox.Show(this, $"'{action.Mod.Name}' does not match its known checksum for '{file.Name}'. " +
                                                                    $"You may have edited the mod outside of Tome. Skip?\n\n" +
                                                                    $"Skipping will de-list the mod from Tome and give you a chance to" +
                                                                    $"back it up while Tome is closed or when it cleans up the installs next launch.",
                                                                    "Checksum mismatch", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                        continue;
                    if (result == MessageBoxResult.Cancel)
                        return null;
                }
            }

            if (!file.Md5Hash.Equals(CryptographyUtil.Md5File(path), StringComparison.InvariantCulture))
            {
                if (MessageBox.Show(this, $"'{action.Mod.Name}' does not match its known checksum for '{file.Name}' and is corrupt. Skip?",
                                          "Checksum mismatch", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
                    return null;
                continue;
            }

            if (Path.GetExtension(file.Name) is ".vpk")
            {
                validatedPaths.Add((path, file));
                continue;
            }

            if (!ArchiveFactory.IsArchive(path, out ArchiveType? archiveType) || archiveType is null)
            {
                if (MessageBox.Show(this, $"'{file.Name}' from '{action.Mod.Name}' is an unknown or unsupported format. Skip?",
                                          "Invalid mod file", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
                    return null;
                continue;
            }

            using IArchive archive = ArchiveFactory.OpenArchive(path);
            IArchiveEntry[] entries = [.. archive.Entries.Where(static entry => Path.GetExtension(entry.Key) is ".vpk")];

            if (entries.Length == 0)
            {
                if (MessageBox.Show(this, $"'{file.Name}' from '{action.Mod.Name}' is empty or contains unsupported files. Skip?",
                                         "Invalid mod archive", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
                    return null;
                continue;
            }

            if (entries.Length > 1)
            {
                if (MessageBox.Show(this, $"'{file.Name}' from '{action.Mod.Name}' contains multiple installable files. " +
                                          $"This is not currently supported. Skip?", "Invalid mod archive",
                                          MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
                    return null;
                continue;
            }

            validatedPaths.Add((path, file));
        }

        if (validatedPaths.Count == 0)
            return [];

        List<InstalledModFile> installedFiles = [];
        foreach ((string path, ModFile file) in validatedPaths)
        {
            string ext = Path.GetExtension(file.Name);
            using FileStream stream = File.OpenRead(path);

            bool result;
            InstalledModFile? installed;
            if (ext is not ".vpk")
            {
                using IArchive archive = ArchiveFactory.OpenArchive(path);
                using Stream entryStream = archive.Entries.First(static entry => Path.GetExtension(entry.Key) is ".vpk").OpenEntryStream();

                (result, installed) = await InstallFileAsync(entryStream, action, file);
            }
            else
            {
                (result, installed) = await InstallFileAsync(stream, action, file);
            }

            if (installed is null)
            {
                if (!result)
                    return null;
                continue;
            }

            installedFiles.Add(installed);
        }

        return installedFiles;
    }
    
    private async Task<(bool, InstalledModFile?)> InstallFileAsync(Stream content, ModAction action, ModFile modFile)
    {
        string tempPath = Path.GetTempFileName();
        try
        {
            using (FileStream writeStream = File.Create(tempPath))
                await content.CopyToAsync(writeStream);

            string addonsFolder = _deadlock.GetAddonsPath(_manifest.GamePath);
            UInt128 modId = action.Mod.Id;
            string targetPath;
    
            if (action.Type is ModActionType.Update)
            {
                ArgumentException.ThrowIfNullOrEmpty(modFile.TargetPath);
                targetPath = modFile.TargetPath;
            }
            else
            {
                string relativePath = Path.GetFileName(modFile.Name);
    
                bool pathFound = false;
                targetPath = string.Empty;
                for (int i = 1; i < 100; i++)
                {
                    targetPath = Path.Combine(addonsFolder, relativePath);
                    string fileName = Path.GetFileName(targetPath);
    
                    if (s_vpkMatchRegex.IsMatch(fileName) && !File.Exists(targetPath))
                    {
                        pathFound = true;
                        break;
                    }
    
                    relativePath = $"pak{i:D2}_dir.vpk";
                }
    
                if (!pathFound)
                {
                    MessageBoxResult result = MessageBox.Show(this, $"You have already installed the maximum amount of mods supported by Tome. Skip?", "Maximum mods installed",
                                                                    MessageBoxButton.OKCancel, MessageBoxImage.Error);
    
                    return (result == MessageBoxResult.OK, null);
                }
            }
    
            File.Copy(tempPath, targetPath, overwrite: true);
            return (true, new InstalledModFile
            {
                RelativePath = Path.GetRelativePath(addonsFolder, targetPath),
                Md5Checksum = CryptographyUtil.Md5File(targetPath),
                GamebananaFile = modFile.GamebananaFile is not null
                    ? new GamebananaFile
                    {
                        Id = modFile.GamebananaFile.Id,
                        Name = modFile.GamebananaFile.Name,
                        UploadDate = modFile.GamebananaFile.Added,
                        ArchiveMd5Checksum = modFile.GamebananaFile.Md5Checksum,
                    } : null
            });
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    private async Task<List<int>> GetCategoryTreeAsync(Category? category)
    {
        if (category is null)
            return [];

        IReadOnlyDictionary<int, CategoryDefinition> results = await _gamebanana.GetFlattenedCategoryDefinitionsAsync(category.Type);

        if (!results.TryGetValue(category.Id, out CategoryDefinition? definition))
            throw new InvalidDataException("Invalid category ID received from Gamebanana API for submission.");

        List<int> ids = [];
        do
        {
            ids.Insert(0, definition.Id);
            results.TryGetValue(definition.ParentId, out definition);
        } while (definition != null);

        return ids;
    }

    private void RebuildConflictableCategories()
    {
        Dictionary<int, List<UInt128>> conflictableMods = [];
        foreach ((UInt128 id, InstalledMod2 mod) in _manifest.Mods2)
        {
            if (mod.GamebananaSubmission is not { Type: "Mod", CategoryBranch.Count: > 0 }
                || !FindConflictableCategory(mod.GamebananaSubmission.CategoryBranch, out int category))
                continue;

            if (!conflictableMods.TryGetValue(category, out List<UInt128>? mods))
                conflictableMods.Add(category, mods = []);

            mods.Add(id);
        }

        _manifest.InstalledConflictableMods2 = conflictableMods;
    }

    private bool FindConflictableCategory(List<int> categoryTree, out int category)
    {
        int categoryTreeLength = categoryTree.Count;
        for (int i = categoryTreeLength - 1; i >= 0; i--)
        {
            int currentCategory = categoryTree[i];
            if (s_conflictableChildCategories.Contains(currentCategory))
            {
                category = currentCategory;
                return true;
            }

            if (currentCategory is SkinSuperCategory && i < categoryTreeLength - 1)
            {
                category = categoryTree[i + 1];
                return true;
            }
        }

        category = 0;
        return false;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) =>
        _confirmTcs.TrySetResult(false);

    private void Continue_Click(object sender, RoutedEventArgs e) =>
        _confirmTcs.TrySetResult(true);

    private void Log(string line) =>
        LogsTextBox.Text += line + '\n';
}
