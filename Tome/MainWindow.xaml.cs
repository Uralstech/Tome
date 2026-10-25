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

using GamebananaAPI.Data.Submission;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Tome.Models;
using Tome.Services;

namespace Tome;

public sealed partial class MainWindow : Window
{
    private readonly FileService _files;
    private readonly DeadlockService _deadlock;
    private readonly GamebananaService _gamebanana;

    private readonly SemaphoreSlim _getSemaphore = new(1, 1);
    private readonly ObservableCollection<ListedMod> _modsList = [];

    private TomeManifest _manifest = null!;
    private MenuItem _defaultFilterMenu = null!;
    private ToggleButton? _currentCategory = null;

    public MainWindow(FileService files, DeadlockService deadlock, GamebananaService gamebanana)
    {
        InitializeComponent();

        _files = files;
        _deadlock = deadlock;
        _gamebanana = gamebanana;
        ModsGrid.ItemsSource = _modsList;
    }

    private void Window_ContentRendered(object sender, EventArgs e)
    {
        if (!InitializeManifest())
            return;

        PatchInstall();
        VerifyManifest();

        FilterChange_Click(null, null);

        FeaturedButton.IsChecked = true;
        SetCurrentCategory(FeaturedButton);
    }

    protected override void OnClosed(EventArgs e)
    {
        _getSemaphore.Dispose();
        base.OnClosed(e);
    }

    #region Core Initialization

    private bool InitializeManifest()
    {
        if (_files.TryGetManifest(out TomeManifest? manifest)
            && _deadlock.VerifyDeadlockInstallation(manifest.GamePath))
        {
            _manifest = manifest;
            return true;
        }

        if (!_deadlock.TryGetDeadlockPath(out string? deadlockPath)
            || !_deadlock.VerifyDeadlockInstallation(deadlockPath))
        {
            MessageBox.Show(this, "Could not find a Deadlock installation. Please select it manually.",
                                  "Could not find Deadlock", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            
            if (!TryRequestDeadlockFolder(null, out deadlockPath)
                || !_deadlock.VerifyDeadlockInstallation(deadlockPath))
            {
                MessageBox.Show(this, "The provided path could not be verified, quitting.",
                                      "Invalid path", MessageBoxButton.OK, MessageBoxImage.Error);

                Application.Current.Shutdown();
                return false;
            }
        }
        else
        {
            MessageBox.Show(this, $"Deadlock successfully detected at: {deadlockPath}",
                                  "Deadlock found", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        manifest ??= new TomeManifest { GamePath = string.Empty };
        manifest.GamePath = deadlockPath;

        _files.SaveManifest(manifest);
        _manifest = manifest;
        return true;
    }

    private void VerifyManifest()
    {
        if (_deadlock.VerifyManifest(_manifest, out ManifestVerificationFailures? failures))
            return;

        MessageBoxResult result = MessageBox.Show(this, $"Multiple warnings while verifying mod list:\n- {string.Join("\n- ", failures.ProblematicMods.Values)}" +
                                                        $"\n\nThis may cause incorrect mod loading and create conflicts. Delete all problematic mods? " +
                                                        $"Problematic mods detected to be *outside* of the game's managed \\addons directory will not be deleted.",
                                                        "Failed mod list verification", MessageBoxButton.YesNo, MessageBoxImage.Error);

        if (result == MessageBoxResult.Yes)
        {
            result = MessageBox.Show(this, $"All problematic mods installed in '{_deadlock.GetAddonsPath(_manifest.GamePath)}' will be deleted " +
                                           "after being backed up. Continue?", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Stop);
        }

        if (result != MessageBoxResult.Yes)
            return;

        SaveFileDialog dialog = new()
        {
            Title = "Backup all problematic mods",
            CheckPathExists = true,
            AddExtension = true,
            FileName = "mods.zip",
            Filter = "ZIP Archive (*.zip)|*.zip"
        };

        if (dialog.ShowDialog() != true)
        {
            MessageBox.Show("Your mods were not deleted as the operation was cancelled.",
                            "Operation cancelled", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _deadlock.ClearManifestFailures(_manifest, failures, dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Operation failed",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PatchInstall(bool isRetry = false)
    {
        try
        {
            _deadlock.PatchDeadlockGameInfo(_manifest.GamePath, out bool corruptWithBackup);
            if (!corruptWithBackup)
                return;

            if (isRetry)
            {
                MessageBox.Show(this, "Please check if Tome has updates or raise an issue on GitHub.", "Patch failed",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBoxResult result = MessageBox.Show(this, "Your Deadlock installation's gameinfo.gi file may be corrupted. " +
                                                            "Restore to a previous version and try patching again?", "Could not patch Deadlock",
                                                            MessageBoxButton.YesNo, MessageBoxImage.Error, MessageBoxResult.Yes);

            if (result != MessageBoxResult.Yes)
                return;

            _deadlock.RestoreGameInfo(_manifest.GamePath);
            MessageBox.Show(this, "gameinfo.gi was restored from a backup. Patching again...", "gameinfo.gi restored",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

            PatchInstall(isRetry: true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not patch Deadlock",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool TryRequestDeadlockFolder(string? existing, [NotNullWhen(true)] out string? path)
    {
        path = null;
        OpenFolderDialog dialog = new()
        {
            InitialDirectory = existing,
            Title = "Select the Deadlock folder"
        };

        if (dialog.ShowDialog() != true)
            return false;

        path = dialog.FolderName;
        return true;
    }

    #endregion

    #region MenuBar

    private void MenuItem_Loaded(object sender, RoutedEventArgs e) =>
        _defaultFilterMenu = (MenuItem)sender;

    private void UpdateGamePath_Click(object sender, RoutedEventArgs e)
    {
        if (!TryRequestDeadlockFolder(_manifest.GamePath, out string? deadlockPath)
            || Path.GetFullPath(deadlockPath) == Path.GetFullPath(_manifest.GamePath))
            return;

        if (!_deadlock.VerifyDeadlockInstallation(deadlockPath))
        {
            MessageBox.Show(this, "The provided path could not be verified.",
                                  "Invalid path", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (_manifest.Skins.Count + _manifest.ModelReplacements.Count + _manifest.Misc.Count > 0)
        {
            MessageBoxResult result = MessageBox.Show(this, "The mods list managed by Tome will be cleared when switching to a different Deadlock installation.",
                                                            "Clear mods list?", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.No)
                return;

            _manifest.Skins.Clear();
            _manifest.ModelReplacements.Clear();
            _manifest.Misc.Clear();
        }

        _manifest.GamePath = deadlockPath;
        _files.SaveManifest(_manifest);

        PatchInstall();
    }

    private void FilterChange_Click(object? sender, RoutedEventArgs? e)
    {
        if (sender is MenuItem menuItem
            && menuItem.Tag is GamebananaAPI.Data.Visibility newFilter)
        {
            _manifest.DefaultFilters = newFilter;
            _files.SaveManifest(_manifest);
        }

        ItemCollection menuItems = _defaultFilterMenu.Items;
        for (int i = 0; i < menuItems.Count; i++)
        {
            ((MenuItem)menuItems[i]).IsChecked =
                (int)_manifest.DefaultFilters == i;
        }
    }

    private void GitHub_Click(object sender, RoutedEventArgs e) =>
        UrlHelper.Open("https://github.com/Uralstech/Tome");

    private void LaunchDeadlock_Click(object sender, RoutedEventArgs e) =>
        _deadlock.LaunchDeadlock();

    #endregion

    #region Search

    private void Search_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Return))
            return;

        e.Handled = true;
        SearchSafe();
    }

    private void Search_Click(object sender, RoutedEventArgs e) =>
        SearchSafe();

    private void SearchSafe()
    {
        SetCurrentCategory(null);
        RunModsUpdateSafe(() =>
            _gamebanana.SearchAsync(SearchBox.Text, _manifest.DefaultFilters));
    }

    #endregion

    #region Categories

    private void Category_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton selectedButton)
            return;

        SetCurrentCategory(selectedButton);

        if (ReferenceEquals(sender, FeaturedButton))
            RunModsUpdateSafe(() => _gamebanana.GetFeaturedAsync(_manifest.DefaultFilters));
        else if (ReferenceEquals(sender, TopSubmissionsButton))
            RunModsUpdateSafe(() => _gamebanana.GetTopSubmissionsAsync(_manifest.DefaultFilters));
    }

    private void SetCurrentCategory(ToggleButton? newChecked)
    {
        if (ReferenceEquals(_currentCategory, newChecked))
            return;

        _currentCategory?.IsChecked = false;
        _currentCategory = newChecked;
    }

    private async void RunModsUpdateSafe(Func<Task<IReadOnlyList<ListedMod>>> func)
    {
        await _getSemaphore.WaitAsync();
        try
        {
            _modsList.Clear();
            foreach (ListedMod mod in await func())
                _modsList.Add(mod);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Operation failed",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _getSemaphore.Release();
        }
    }

    #endregion

    #region Mod Management

    private void ModsInfo_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button
            || button.Tag is not ListedMod mod)
            return;

        ShowModInfoSafeAsync(mod.Id, mod.Type);
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
            MessageBox.Show(this, ex.Message, "Operation failed",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _getSemaphore.Release();
        }
    }

    #endregion
}