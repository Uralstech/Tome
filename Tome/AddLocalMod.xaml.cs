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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Tome.Data;
using Tome.Models;

namespace Tome;

public partial class AddLocalMod : Window
{
    private readonly TomeManifest _manifest;
    private readonly ModActions _pendingActions;
    public UInt128? Replacing { get; private set; }

    public AddLocalMod(TomeManifest manifest, ModActions pendingActions)
    {
        InitializeComponent();

        _manifest = manifest;
        _pendingActions = pendingActions;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        List<ModReplacing> replacements = new(_manifest.Mods2.Count + 1)
        {
            new("Install as new", null)
        };

        foreach ((UInt128 id, InstalledMod2 mod) in _manifest.Mods2)
        {
            if (_pendingActions.TryGetValue(id, out ModAction? pending)
                && pending.Type is not ModActionType.Update)
                continue;

            replacements.Add(new ModReplacing(mod.Name, id));
        }

        ModReplacement.ItemsSource = replacements;
        ModReplacement.SelectedIndex = 0;
    }

    private void ModReplacement_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems is not { Count: > 0 }
            || e.AddedItems[0] is not ModReplacing replacing)
            return;

        Replacing = replacing.ModId;
        ModName.IsEnabled = ModAuthor.IsEnabled = replacing.ModId is null;

        if (replacing.ModId is UInt128 modId
            && _manifest.Mods2.TryGetValue(modId, out InstalledMod2? installedMod))
        {
            ModName.Text = installedMod.Name;
            ModAuthor.Text = installedMod.Author;
        }
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Title = "Select the mod file/archive",
            CheckPathExists = true,
            AddExtension = true,
            Filter = "Valve Pack (*.vpk)|*.vpk|" +
                     "RAR Archive (*.rar)|*.rar|" +
                     "ZIP Archive (*.zip)|*.zip|" +
                     "TAR Archive (*.tar)|*.tar|" +
                     "GZip Archive (*.gz)|*.gz|" +
                     "7-Zip Archive (*.7z)|*.7z|" +
                     "All Supported Files (*.vpk;*.rar;*.zip;*.tar;*.gz;*.7z)|*.vpk;*.rar;*.zip;*.tar;*.gz;*.7z|" +
                     "All Files (*.*)|*.*",
            FilterIndex = 7,
        };

        if (dialog.ShowDialog() == true)
            ModPath.Text = dialog.FileName;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(ModName.Text))
        {
            MessageBox.Show(this, "Please enter a name for the mod to continue.", "Empty name",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!File.Exists(ModPath.Text))
        {
            MessageBox.Show(this, "Please enter a valid path for the mod to continue.", "Invalid path",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        DialogResult = true;
        Close();
    }
}
