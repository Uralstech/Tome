using GamebananaAPI.Data.Submission;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Tome.Models;
using Tome.Services;

namespace Tome;

public sealed partial class MainWindow : Window
{
    private readonly SemaphoreSlim _getSemaphore = new(1, 1);

    private readonly FileService _files;
    private readonly DeadlockService _deadlock;
    private readonly GamebananaService _gamebanana;

    private readonly ObservableCollection<ListedMod> _mods = [];
    private InstallationsManifest? _installations = null;
    private ToggleButton? _currentChecked = null;

    public MainWindow(FileService files, DeadlockService deadlock, GamebananaService gamebanana)
    {
        InitializeComponent();

        _files = files;
        _deadlock = deadlock;
        _gamebanana = gamebanana;
        ModsGrid.ItemsSource = _mods;
    }

    protected override void OnClosed(EventArgs e)
    {
        _getSemaphore.Dispose();

        base.OnClosed(e);
    }

    private void InitializeManifest()
    {
        if (_files.TryGetManifest(out InstallationsManifest? manifest)
            && _deadlock.VerifyDeadlockInstallation(manifest.GamePath))
        {
            _installations = manifest;
            return;
        }

        if (!_deadlock.TryGetDeadlockPath(out string? deadlockPath)
            || !_deadlock.VerifyDeadlockInstallation(deadlockPath))
        {
            MessageBox.Show(this, "Could not find a Deadlock installation. Please select it manually.", "Could not find Deadlock", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            deadlockPath = RequestDeadlockFolder(null);

            if (!_deadlock.VerifyDeadlockInstallation(deadlockPath))
            {
                MessageBox.Show(this, "The provided path could not be verified.", "Invalid path", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }
        }
        else
        {
            MessageBox.Show(this, $"Deadlock successfully detected at: {deadlockPath}", "Deadlock found", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        manifest = new InstallationsManifest()
        {
            GamePath = deadlockPath
        };

        _files.SaveManifest(manifest);
        _installations = manifest;
    }

    private void VerifyManifest()
    {
        if (_deadlock.VerifyManifest(_installations!, out IReadOnlyList<string>? warnings))
            return;

        MessageBoxResult result = MessageBox.Show(this, $"Multiple warnings while verifying mod list:\n- {string.Join("\n- ", warnings)}" +
                                                        $"\nThis may cause incorrect mod loading and create conflicts. Clear all existing mods?",
                                                        "Failed mod list verification", MessageBoxButton.YesNo, MessageBoxImage.Error);

        if (result == MessageBoxResult.Yes)
            result = MessageBox.Show(this, "All mods installed with your Deadlock installation will be deleted. " +
                                           "Your addons/ directory containing the mods will be backed up to a folder of your choice. " +
                                           "Continue?", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Stop);

        if (result != MessageBoxResult.Yes)
            return;
        
        SaveFileDialog dialog = new()
        {
            Title = "Save the backup",
            CheckPathExists = true,
            AddExtension = true,
            FileName = "mods.zip",
            Filter = "ZIP Archive (*.zip)|*.zip"
        };

        if (dialog.ShowDialog() != true)
        {
            MessageBox.Show("Your files were not deleted as the operation was cancelled.",
                            "Operation cancelled", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _deadlock.ClearAllMods(_installations!, dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                ex.Message, "Operation failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PatchInstall()
    {
        try
        {
            _deadlock.PatchDeadlockGameInfo(_installations!.GamePath, out bool corruptWithBackup);
            if (!corruptWithBackup)
                return;

            MessageBoxResult result = MessageBox.Show(this, "Your Deadlock installation's gameinfo.gi file may be corrupted. " +
                                                            "Restore to a previous version and try patching again? " +
                                                            "If this happens frequently, Tome may need an update. " +
                                                            "Otherwise, raise an issue on Github.", "Could not patch Deadlock",
                                                            MessageBoxButton.YesNo, MessageBoxImage.Error, MessageBoxResult.Yes);

            if (result != MessageBoxResult.Yes)
                return;

            _deadlock.RestoreGameInfo(_installations.GamePath);
            MessageBox.Show(this, "gameinfo.gi was restored from a backup. Patching again...", "gameinfo.gi restored",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

            PatchInstall();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                ex.Message, "Could not patch Deadlock",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string? RequestDeadlockFolder(string? existing)
    {
        OpenFolderDialog dialog = new()
        {
            FolderName = existing,
            Title = "Select the Deadlock folder"
        };

        return dialog.ShowDialog() == true
            ? dialog.FolderName : null;
    }

    private void SetChecked(ToggleButton? newChecked)
    {
        if (ReferenceEquals(_currentChecked, newChecked))
            return;

        _currentChecked?.IsChecked = false;
        _currentChecked = newChecked;
    }

    private void SearchSafe()
    {
        SetChecked(null);
        RunModsUpdateSafe(() => _gamebanana.SearchAsync(SearchBox.Text));
    }

    private async void ShowModInfoSafeAsync(int id, string type)
    {
        await _getSemaphore.WaitAsync();
        try
        {
            SubmissionProfile submission = await _gamebanana.GetSubmissionAsync(id, type);
            ModInfo modInfoWindow = new(submission);
            modInfoWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                ex.Message,
                "Operation failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _getSemaphore.Release();
        }
    }

    private async void RunModsUpdateSafe(Func<Task<IReadOnlyList<ListedMod>>> func)
    {
        await _getSemaphore.WaitAsync();
        try
        {
            _mods.Clear();
            foreach (ListedMod mod in await func())
                _mods.Add(mod);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                ex.Message,
                "Operation failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _getSemaphore.Release();
        }
    }

    private void Window_ContentRendered(object sender, EventArgs e)
    {
        InitializeManifest();
        PatchInstall();
        VerifyManifest();

        FeaturedButton.IsChecked = true;
        SetChecked(FeaturedButton);
    }

    private void Search_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key is System.Windows.Input.Key.Enter or System.Windows.Input.Key.Return)
        {
            e.Handled = true;
            SearchSafe();
        }
    }

    private void Search_Click(object sender, RoutedEventArgs e) => SearchSafe();

    private void InfoButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button
            || button.Tag is not ListedMod mod)
            return;

        ShowModInfoSafeAsync(mod.Id, mod.Type);
    }

    private void Category_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton selectedButton)
            return;

        SetChecked(selectedButton);

        if (ReferenceEquals(sender, FeaturedButton))
            RunModsUpdateSafe(() => _gamebanana.GetFeaturedAsync());
        else if (ReferenceEquals(sender, TopSubmissionsButton))
            RunModsUpdateSafe(() => _gamebanana.GetTopSubmissionsAsync());
    }

    private void GitHub_Click(object sender, RoutedEventArgs e)
    {
        using Process? _ = Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/Uralstech/Tome",
            UseShellExecute = true
        });
    }

    private void LaunchDeadlock_Click(object sender, RoutedEventArgs e)
    {
        using Process? proc = _deadlock.LaunchDeadlock();
        if (proc is null)
        {
            MessageBox.Show("Could not start Deadlock.");
            return;
        }
    }
}