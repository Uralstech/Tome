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
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Tome.Data;
using Tome.Services;

namespace Tome.Models;

public abstract record ListedMod(
    UInt128 Id,
    string Name,
    string? Author,
    int Likes,
    bool IsObsolete,
    bool IsLocal) : INotifyPropertyChanged
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
    }

    public bool HasUpdate
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

    public bool CanUpdate => Install && HasUpdate;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record OnlineListedMod : ListedMod
{
    public int SubmissionId { get; }
    public string SubmissionType { get; }

    public OnlineListedMod(int submissionId, string submissionType,
        string name, string author, int likes, bool isObsolete)
        : base((UInt128)submissionId, name, author, likes, isObsolete, IsLocal: false)
    {
        SubmissionId = submissionId;
        SubmissionType = submissionType;
    }

    public OnlineListedMod(SubmissionProfile submission, string type)
        : this(submission.Id, type,
              submission.Name, submission.Author.Name,
              submission.Likes, submission.IsObsolete)
    { }
}

public sealed record OfflineListedMod : ListedMod
{
    public string Path { get; }

    public OfflineListedMod(UInt128 id, string path, string name, string? author)
        : base(id, name, author, Likes: 0, IsObsolete: false, IsLocal: true)
    {
        Path = path;
    }

    public OfflineListedMod(UInt128 id, InstalledMod2 installedMod, TomeManifest manifest)
        : base(id, installedMod.Name, installedMod.Author, Likes: 0, IsObsolete: false, IsLocal: true)
    {
        if (installedMod.GamebananaSubmission is not null
            || installedMod.Files is not { Count: 1 })
            throw new InvalidOperationException("Cannot create offline listing from online or multi-file mod.");

        DeadlockService deadlock = App.Services.GetRequiredService<DeadlockService>();
        string gamePath = deadlock.GetAddonsPath(manifest.GamePath);
        string relativePath = installedMod.Files[0].RelativePath;

        Path = System.IO.Path.Combine(gamePath, relativePath);
    }
}

public sealed class ModActions : Dictionary<UInt128, ModAction>;
public sealed record ModAction(ModActionType Type, ListedMod Mod);

public sealed record ModReplacing(string Text, UInt128? ModId);

public sealed record OnlineModFileModel(string Text, string? Group, SubmissionFile? File)
{
    public bool IsSelected { get; set; }
    public bool IsNotSelected => !IsSelected;
}

public sealed record ModFile(
    Uri Source,
    string Name,
    string Md5Hash,
    long Size,

    string? TargetPath,
    SubmissionFile? GamebananaFile
);

public enum ModActionType
{
    Install,
    Update,
    Delete,
}
