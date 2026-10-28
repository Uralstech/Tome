using GamebananaAPI.Data.Submission;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Text;
using System.Windows;
using Tome.Models;
using Tome.Services;

namespace Tome;

public partial class Downloads : Window
{
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
        Queue<(ModAction, ModFile)>? actionsQueue = await LoadSubmissionsAsync();
        if (actionsQueue is null)
        {
            Close();
            return;
        }

        DialogResult = await ExecuteActionsAsync(actionsQueue);
        Close();
    }

    #region File Selection

    private async Task<Queue<(ModAction, ModFile)>?> LoadSubmissionsAsync()
    {
        try
        {
            CancelContinueButtons.IsEnabled = false;
            CancelContinueButtons.Visibility = Visibility.Visible;
            
            Log("Loading submission details, please wait...");

            StringBuilder sb = new();
            sb.AppendLine("The following actions have been requested:");

            Queue<(ModAction, ModFile)> queue = [];
            string addonsFolder = _deadlock.GetAddonsPath(_manifest.GamePath);

            foreach ((int id, ModAction action) in _requestedActions)
            {
                string? targetName = action.Type is ModActionType.Update or ModActionType.Delete
                    ? Path.GetFileName(_manifest.Mods[id].RelativePath) : null;

                ModFile file;
                FileInfo fileInfo;
                switch (action.Type)
                {
                    case ModActionType.Update:
                    case ModActionType.Install:
                        if (action.Mod.IsLocal)
                        {
                            fileInfo = new FileInfo(action.Mod.Path!);
                            file = new ModFile(
                                Source: new Uri(fileInfo.FullName, UriKind.Absolute),
                                Md5: CryptographyUtil.Md5File(fileInfo.FullName),
                                TargetName: targetName,
                                Size: fileInfo.Length
                            );
                            break;
                        }

                        SubmissionProfile submission = await _gamebanana.GetSubmissionAsync(id, action.Mod.SubmissionType!);
                        if (submission.Files is not { Count: > 0 })
                        {
                            MessageBoxResult result = MessageBox.Show(this, $"'{submission.Name}' does not have any downloadable file. Skip?", "Invalid mod",
                                                                            MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                            if (result == MessageBoxResult.OK)
                                continue;

                            return null;
                        }

                        SubmissionFile remoteFile = submission.Files.Count > 1
                            ? await AskUserForFileAsync(submission)
                            : submission.Files[0];

                        file = new ModFile(
                            Source: new Uri(remoteFile.DownloadUrl),
                            Md5: remoteFile.Md5Checksum,
                            TargetName: targetName,
                            Size: remoteFile.Size
                        );
                        break;

                    case ModActionType.Delete:
                        InstalledMod registeredMod = _manifest.Mods[id];
                        string fullPath = Path.Combine(addonsFolder, registeredMod.RelativePath);
                        fileInfo = new FileInfo(fullPath);

                        file = new ModFile(
                            Source: new Uri(fileInfo.FullName, UriKind.Absolute),
                            Md5: registeredMod.OriginalFileMd5Checksum,
                            TargetName: targetName,
                            Size: fileInfo.Length
                        );
                        break;

                    default:
                        throw new NotImplementedException($"Case for '{action.Type}' not implemented.");
                }

                sb.AppendLine($"- {action.Type} '{action.Mod.Name ?? "[no name provided]"}'");
                queue.Enqueue((action, file));
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

    private async Task<SubmissionFile> AskUserForFileAsync(SubmissionProfile submission)
    {
        FileSelectionText.Text = $"'{submission.Name}' has multiple files. Choose one to install:";
        _fileSelectionTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        List<OnlineModFileModel> options = [.. submission.Files!.Select(static file =>
            new OnlineModFileModel()
            {
                Text = !string.IsNullOrEmpty(file.Description)
                            ? $"{file.Name}: {file.Description}" : file.Name,
                File = file,
            })
        ];

        options[0].IsSelected = true;
        FileSelectionButtons.ItemsSource = options;

        FileSelectionBorder.Visibility = Visibility.Visible;
        await _fileSelectionTcs.Task;

        OnlineModFileModel result = options.First(static file => file.IsSelected);
        FileSelectionBorder.Visibility = Visibility.Collapsed;
        return result.File;
    }

    private void FileSelection_Click(object sender, RoutedEventArgs e) =>
        _fileSelectionTcs?.TrySetResult();

    #endregion

    private async Task<bool> ExecuteActionsAsync(Queue<(ModAction, ModFile)> queue)
    {
        DownloadProgressBorder.Visibility = Visibility.Visible;
        DownloadProgressBar.Minimum = 0;
        DownloadProgressBar.Maximum = 1;

        foreach ((ModAction action, ModFile file) in queue)
        {
            switch (action.Type)
            {
                case ModActionType.Update:
                case ModActionType.Install:
                    CurrentDownloadText.Text = $"Downloading {action.Mod.Name ?? file.Source.ToString()}...";
                    DownloadProgressBar.Value = 0;

                    string path = Path.GetTempFileName();
                    string? contentType = await _gamebanana.DownloadAsync(file.Source, path, progress =>
                        DownloadProgressBar.Value = (double)progress / file.Size);

                    break;

                case ModActionType.Delete:
                    CurrentDownloadText.Text = $"Deleting {action.Mod.Name ?? file.TargetName}...";
                    DownloadProgressBar.Value = 1;

                    string filePath = file.Source.LocalPath;
                    string md5 = CryptographyUtil.Md5File(filePath);

                    if (!md5.Equals(file.Md5, StringComparison.InvariantCulture))
                    {
                        MessageBoxResult result = MessageBox.Show(this, $"'{action.Mod.Name ?? file.TargetName}' does not match its known checksum. " +
                                                                        $"You may have edited the mod outside of Tome. Continue with deletion?", "Checksum mismatch",
                                                                        MessageBoxButton.YesNo, MessageBoxImage.Warning);

                        if (result == MessageBoxResult.No)
                            continue;
                    }

                    File.Delete(filePath);
                    break;

                default:
                    throw new NotImplementedException($"Case for '{action.Type}' not implemented.");
            }
        }

        return true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) =>
        _confirmTcs.TrySetResult(false);

    private void Continue_Click(object sender, RoutedEventArgs e) =>
        _confirmTcs.TrySetResult(true);

    private void Log(string line) =>
        LogsTextBox.Text += line + '\n';
}
