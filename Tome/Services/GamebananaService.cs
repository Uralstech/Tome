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

using GamebananaAPI;
using GamebananaAPI.Data;
using GamebananaAPI.Data.Categories;
using GamebananaAPI.Data.Game.Featured;
using GamebananaAPI.Data.Game.Search;
using GamebananaAPI.Data.Game.TopSubs;
using GamebananaAPI.Data.Submission;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using Tome.Models;

namespace Tome.Services;

public sealed class GamebananaService : IDisposable
{
    private const int DeadlockGameId = 20948;

    private static readonly TimeSpan s_topLevelCategoryDefinitionsCacheTime = TimeSpan.FromHours(1);
    private static readonly TimeSpan s_flatCategoryDefinitionsCacheTime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan s_submissionCacheTime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan s_featuredCacheTime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan s_topSubmissionsCacheTime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan s_searchCacheTime = TimeSpan.FromSeconds(30);

    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    #region Categories

    public async Task<IReadOnlyDictionary<string, TopLevelCategoryDefinition>> GetTopLevelCategoryDefinitionsAsync()
    {
        static string GetCacheKey() => "topLevelCategoryDefinitions";

        if (_cache.TryGetValue(GetCacheKey(),
            out IReadOnlyDictionary<string, TopLevelCategoryDefinition>? cachedCategoryDefinitions))
            return cachedCategoryDefinitions!;

        List<TopLevelCategoryDefinition> flatCategoryDefinitions = await Gamebanana.Games.Category.GetCategoryDefinitionsAsync(DeadlockGameId);
        Dictionary<string, TopLevelCategoryDefinition> categoryDefinitions = new(flatCategoryDefinitions.Count);

        foreach (TopLevelCategoryDefinition categoryDefinition in flatCategoryDefinitions)
        {
            if (!categoryDefinitions.TryAdd(categoryDefinition.CategoryType, categoryDefinition))
                throw new FormatException("Duplicate category types were returned by Gamebanana APIs.");
        }

        _cache.Set(GetCacheKey(), categoryDefinitions, s_topLevelCategoryDefinitionsCacheTime);
        return categoryDefinitions;
    }

    public async Task<IReadOnlyDictionary<int, CategoryDefinition>> GetFlattenedCategoryDefinitionsAsync(string categoryType)
    {
        string GetCacheKey() => $"flatCategoryDefinitions:{categoryType}";

        static void AddCategoriesRecursive(Dictionary<int, CategoryDefinition> dict, CategoryDefinition categoryDefinition)
        {
            if (!dict.TryAdd(categoryDefinition.Id, categoryDefinition))
                throw new FormatException("Duplicate category IDs were returned by Gamebanana APIs.");

            if (categoryDefinition.Children is not { Count: > 0 })
                return;

            foreach (CategoryDefinition childCategoryDefinition in categoryDefinition.Children)
                AddCategoriesRecursive(dict, childCategoryDefinition);
        }

        if (_cache.TryGetValue(GetCacheKey(),
            out IReadOnlyDictionary<int, CategoryDefinition>? cachedCategoryDefinitions))
            return cachedCategoryDefinitions!;

        List<CategoryDefinition> categoryDefinitions = await Gamebanana.Category.GetCategoryDefinitionsAsync(categoryType, DeadlockGameId);
        Dictionary<int, CategoryDefinition> flatCategoryDefinitions = [];

        foreach (CategoryDefinition categoryDefinition in categoryDefinitions)
            AddCategoriesRecursive(flatCategoryDefinitions, categoryDefinition);

        _cache.Set(GetCacheKey(), flatCategoryDefinitions, s_flatCategoryDefinitionsCacheTime);
        return flatCategoryDefinitions;
    }

    #endregion

    #region Submissions

    public async Task<SubmissionProfile> GetSubmissionAsync(int id, string type)
    {
        string GetCacheKey() => $"submissionProfile:{id}";

        if (!_cache.TryGetValue(GetCacheKey(), out SubmissionProfile? profile))
        {
            profile = await Gamebanana.Submission.PropertiesAsync(id, type);
            _cache.Set(GetCacheKey(), profile, s_submissionCacheTime);
        }

        return profile!.Files is { Count: > 0 }
            ? profile : throw new InvalidOperationException("Cannot use mod with no listed files.");
    }

    public Task<string?> DownloadAsync(Uri uri, string outputPath, Action<long>? onProgress = null) =>
        Gamebanana.Submission.DownloadFileAsync(uri, outputPath, onProgress);

    #endregion

    #region Search/Listings

    private ListedMod BuildListedMod(int id,
        TomeManifest manifest, ModActions pendingActions,
        string? submissionType = null, string? path = null,
        bool isObsolete = false, bool isLocal = false,
        string? name = null, string? author = null, int likes = 0)
    {
        bool hasPendingAction = pendingActions.TryGetValue(id, out ModAction? pendingAction);

        return new(
            Id: id,
            SubmissionType: submissionType,
            Path: path,
            IsObsolete: isObsolete,
            IsLocal: isLocal,
            Name: name,
            Author: author,
            Likes: likes,
            Install: hasPendingAction
                ? pendingAction!.Type is ModActionType.Install or ModActionType.Update
                : manifest.Mods.ContainsKey(id),
            HasUpdate: (hasPendingAction && pendingAction!.Type is ModActionType.Update)
                        || false // TODO!
        ) {
            Update = hasPendingAction && pendingAction!.Type is ModActionType.Update,
        };
    }

    public async Task<IEnumerable<ListedMod>> GetInstalledSubmissionsAsync(TomeManifest manifest, ModActions pendingActions)
    {
        List<ListedMod> list = new(manifest.Mods.Count);
        foreach ((int id, InstalledMod mod) in manifest.Mods)
        {
            if (mod.GamebananaMetadata is GamebananaMetadata gamebanana)
            {
                SubmissionProfile submission = await GetSubmissionAsync(gamebanana.SubmissionId, gamebanana.SubmissionType);
                list.Add(BuildListedMod(
                    id: gamebanana.SubmissionId,
                    submissionType: gamebanana.SubmissionType,
                    isObsolete: submission.IsObsolete,

                    name: submission.Name,
                    author: submission.Author.Name,
                    likes: submission.Likes,
                    
                    manifest: manifest, pendingActions: pendingActions
                ));

                continue;
            }

            list.Add(BuildListedMod(
                id: id,
                path: mod.RelativePath,
                isLocal: true,

                name: mod.ListingMetadata?.Name,
                author: mod.ListingMetadata?.Author,

                manifest: manifest, pendingActions: pendingActions
            ));
        }

        return list;
    }

    public async Task<IEnumerable<ListedMod>> SearchAsync(string unformattedQuery,
        Visibility defaultVisibility, TomeManifest manifest, ModActions pendingActions)
    {
        string[] parts = unformattedQuery.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        StringBuilder query = new();

        string? sort = null;
        string? includeTypes = null;
        string? excludeTypes = null;
        Visibility visibility = defaultVisibility;

        foreach (string sub in parts)
        {
            if (!sub.StartsWith('$'))
            {
                query.Append(sub)
                     .Append(' ');
                continue;
            }

            int split = sub.IndexOf(':');
            if (split <= 1)
            {
                query.Append(sub)
                     .Append(' ');
                continue;
            }

            string param = sub[(split + 1)..];
            switch (sub[..split])
            {
                case "$typeId":
                    string[] typeId = param.Split(',');
                    if (typeId.Length != 2 || !int.TryParse(typeId[1], out int id))
                        throw new FormatException("typeId must be [submission type],[numerical submission ID]");

                    string type = typeId[0];
                    if (type is not ("Sound" or "Mod"))
                        throw new NotSupportedException("Unknown or unsupported submission type.");

                    SubmissionProfile submission = await GetSubmissionAsync(id, type);
                    return [BuildListedMod(
                        id: submission.Id,
                        submissionType: type,
                        isObsolete: submission.IsObsolete,

                        name: submission.Name,
                        author: submission.Author.Name,
                        likes: submission.Likes,
                        
                        manifest: manifest, pendingActions: pendingActions
                    )];

                case "$link":
                    return await ParseLinkAsync(param, manifest, pendingActions);

                case "$sort":
                    sort = param; break;

                case "$types":
                    includeTypes = param; break;

                case "$^types":
                    excludeTypes = param; break;

                case "$show":
                    visibility = param switch
                    {
                        "all" => Visibility.Hide,
                        "warn" => Visibility.Warn,
                        "default" => defaultVisibility,
                        _ => visibility,
                    }; break;
            }
        }

        string queryText = query.ToString().Trim();
        if (string.IsNullOrEmpty(queryText))
            return [];

        if (queryText.Length < 3)
            throw new Exception("Search query needs to be at least 3 characters long.");

        string GetCacheKey() => $"search:{queryText}:{sort}:{includeTypes}:{excludeTypes}:{visibility}";

        if (!_cache.TryGetValue(GetCacheKey(), out SearchSubmissions? submissions))
        {
            submissions = await Gamebanana.Games.Search.SearchAsync(DeadlockGameId, queryText,
                                sort, includeTypes, excludeTypes);

            _cache.Set(GetCacheKey(), submissions, s_searchCacheTime);
        }

        return submissions!.Submissions
            .Where(sub => sub.Type is "Sound" or "Mod" && (int)sub.SafeForWork <= (int)visibility && sub.HasFiles)
            .Select(sub => BuildListedMod(
                id: sub.Id,
                submissionType: sub.Type,
                isObsolete: sub.IsObsolete,

                name: sub.Name,
                author: sub.Author.Name,
                likes: sub.Likes,

                manifest: manifest, pendingActions: pendingActions
            ));
    }

    public async Task<IEnumerable<ListedMod>> ParseLinkAsync(string link,
        TomeManifest manifest, ModActions pendingActions)
    {
        if (!Uri.TryCreate(link, UriKind.Absolute, out Uri? uri))
            throw new ArgumentException("Invalid submission link.");

        string path = uri.AbsolutePath.Trim('/');
        int splitIndex = path.IndexOf('/');
        if (splitIndex < 1 || splitIndex != path.LastIndexOf('/'))
            throw new ArgumentException("Invalid submission link.");

        string type;
        string uriType = path[..splitIndex];

        if (uriType.CompareTo("sounds", StringComparison.InvariantCultureIgnoreCase) == 0)
            type = "Sound";
        else if (uriType.CompareTo("mods", StringComparison.InvariantCultureIgnoreCase) == 0)
            type = "Mod";
        else
            throw new NotSupportedException("Unknown or unsupported submission type.");

        if (!int.TryParse(path[(splitIndex + 1)..], out int id))
            throw new ArgumentException("Invalid submission ID.");

        SubmissionProfile submission = await GetSubmissionAsync(id, type);
        return [BuildListedMod(
            id: submission.Id,
            submissionType: type,
            isObsolete: submission.IsObsolete,
            
            name: submission.Name,
            author: submission.Author.Name,
            likes: submission.Likes,
            
            manifest: manifest, pendingActions: pendingActions
        )];
    }

    public async Task<IEnumerable<ListedMod>> GetTopSubmissionsAsync(Visibility defaultVisibility,
        TomeManifest manifest, ModActions pendingActions)
    {
        static int GetPriority(TopSubmissionPeriod group) => group switch
        {
            TopSubmissionPeriod.Today => 0,
            TopSubmissionPeriod.ThisWeek => 1,
            TopSubmissionPeriod.ThisMonth => 2,
            TopSubmissionPeriod.ThreeMonths => 3,
            TopSubmissionPeriod.SixMonths => 4,
            TopSubmissionPeriod.ThisYear => 5,
            TopSubmissionPeriod.AllTime => 6,
            _ => int.MaxValue
        };

        string GetCacheKey() => $"topSubmissions:{defaultVisibility}";

        if (!_cache.TryGetValue(GetCacheKey(), out IReadOnlyList<TopSubmission>? top))
        {
            top = await Gamebanana.Games.Search.TopSubmissionsAsync(DeadlockGameId);
            _cache.Set(GetCacheKey(), top, s_topSubmissionsCacheTime);
        }

        return top!
            .OrderBy(static sub => GetPriority(sub.Period))
            .Where(sub => sub.Type is "Sound" or "Mod" && (int)sub.SafeForWork <= (int)defaultVisibility)
            .Select(sub => BuildListedMod(
                id: sub.Id,
                submissionType: sub.Type,

                name: sub.Name,
                author: sub.Author.Name,
                likes: sub.Likes,

                manifest: manifest, pendingActions: pendingActions
            ));
    }

    public async Task<IEnumerable<ListedMod>> GetFeaturedAsync(Visibility defaultVisibility,
        TomeManifest manifest, ModActions pendingActions)
    {
        static int GetPriority(FeaturingGroup group) => group switch
        {
            FeaturingGroup.Today => 0,
            FeaturingGroup.Yesterday => 1,
            FeaturingGroup.ThreeDays => 2,
            _ => int.MaxValue
        };

        string GetCacheKey() => $"featured:{defaultVisibility}";

        if (!_cache.TryGetValue(GetCacheKey(), out FeaturedSubmissions? featured))
        {
            featured = await Gamebanana.Games.Search.FeaturedAsync(DeadlockGameId);
            _cache.Set(GetCacheKey(), featured, s_featuredCacheTime);
        }

        return featured!.Submissions
            .OrderBy(static sub =>
                GetPriority(sub.Featurings
                .OrderBy(static feat => GetPriority(feat.Group))
                .ThenByDescending(static feat => feat.Date)
                .FirstOrDefault()?.Group ?? (FeaturingGroup)int.MaxValue))
            .Where(sub => sub.Type is "Sound" or "Mod" && (int)sub.SafeForWork <= (int)defaultVisibility && sub.HasFiles)
            .Select(sub => BuildListedMod(
                id: sub.Id,
                submissionType: sub.Type,
                isObsolete: sub.IsObsolete,

                name: sub.Name,
                author: sub.Author.Name,
                likes: sub.Likes,

                manifest: manifest, pendingActions: pendingActions
            ));
    }

    #endregion

    public void Dispose() => _cache.Dispose();
}
