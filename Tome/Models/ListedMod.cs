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

public sealed class ListedMod : INotifyPropertyChanged
{
    public required int Id { get; init; }
    public required string Type { get; init; }
    public required string Name { get; init; }
    public required string Author { get; init; }
    public required bool IsObsolete { get; init; }
    public required int Likes { get; init; }

    public required bool Installed
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
    }

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

    public required bool HasUpdate { get; init; }

    public bool CanUpdate => Installed && HasUpdate;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public ListedModMinimal Minimal() => new(Id, Type);
}

public record ListedModMinimal(int Id, string Type);

public enum ModAction
{
    Install,
    Update,
    Delete,
}
