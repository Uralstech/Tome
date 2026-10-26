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

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Tome.Models;

public sealed record ListedMod(
    int Id,
    string? SubmissionType,
    string? RelativePath,
    bool IsObsolete,
    bool IsLocal,
    string? Name,
    string? Author,
    int Likes,
    bool Install,
    bool HasUpdate) : INotifyPropertyChanged
{
    public bool Install
    {
        get;
        set
        {
            if (field == value)
                return;

            if (!(field = value))
                Update = false;

            OnPropertyChanged();
            OnPropertyChanged(nameof(CanUpdate));
        }
    } = Install;

    public bool Update
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            OnPropertyChanged();
        }
    }

    public bool CanUpdate => Install && !IsLocal && HasUpdate;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public ModAction ToAction(ModActionType type) =>
        new(type, SubmissionType, RelativePath, IsLocal);
}

public sealed class ModActions : Dictionary<int, ModAction>;

public sealed record ModAction(
    ModActionType Type,
    string? SubmissionType,
    string? RelativePath,
    bool IsLocal);

public enum ModActionType
{
    Install,
    Update,
    Delete,
}
