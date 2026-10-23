using GamebananaAPI;
using GamebananaAPI.Data.Game;
using GamebananaAPI.Data.Game.Featured;
using GamebananaAPI.Data.Game.Search;
using GamebananaAPI.Data.Game.TopSubs;
using GamebananaAPI.Data.Submission;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using Tome.Models;

namespace Tome.Services;

public class GamebananaService : IDisposable
{
    private const int DeadlockGameId = 20948;

    private static readonly TimeSpan s_submissionCacheTime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan s_featuredCacheTime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan s_topSubmissionsCacheTime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan s_searchCacheTime = TimeSpan.FromSeconds(30);

    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public async Task<SubmissionProfile> GetSubmissionAsync(int id, string type)
    {
        string GetCacheKey() => $"submissionProfile:{id}";

        if (!_cache.TryGetValue(GetCacheKey(), out object? cachedVal)
            || cachedVal is not SubmissionProfile profile)
        {
            profile = await Gamebanana.Submission.PropertiesAsync(id, type);
            _cache.Set(GetCacheKey(), profile, s_submissionCacheTime);
        }

        return profile.Files is { Count: > 0 }
            ? profile : throw new InvalidOperationException("Cannot use mod with no listed files.");
    }

    public async Task<IReadOnlyList<Mod>> SearchAsync(string unformattedQuery, Visibility defaultVisibility = Visibility.Show)
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
                    return [new Mod()
                    {
                        Id = submission.Id,
                        Type = type,
                        Name = submission.Name,
                        Likes = submission.Likes,
                        Author = submission.Author.Name
                    }];

                case "$link":
                    return await ParseLinkAsync(param);

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
                        "warned" => Visibility.Warn,
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

        if (_cache.TryGetValue(GetCacheKey(), out object? cachedVal)
            && cachedVal is IReadOnlyList<Mod> cachedMods)
            return cachedMods;

        SearchSubmissions submissions = await Gamebanana.Games.Search.SearchAsync(DeadlockGameId, queryText,
            sort, includeTypes, excludeTypes);

        Mod[] mods = submissions.Submissions
            .Where(sub => sub.Type is "Sound" or "Mod" && (int)sub.SafeForWork <= (int)visibility && sub.HasFiles)
            .Select(static sub => new Mod()
            {
                Id = sub.Id,
                Type = sub.Type,
                Name = sub.IsObsolete
                    ? "<OBSOLETE> " + sub.Name : sub.Name,
                Likes = sub.Likes,
                Author = sub.Author.Name
            }).ToArray();

        _cache.Set(GetCacheKey(), mods, s_searchCacheTime);
        return mods;
    }

    public async Task<IReadOnlyList<Mod>> ParseLinkAsync(string link)
    {
        if (!Uri.TryCreate(link, UriKind.Absolute, out Uri? uri))
            throw new ArgumentException("Invalid submission link.");

        string path = uri.AbsolutePath.Trim('/');
        int splitIndex = path.IndexOf('/');
        if (splitIndex < 1 || splitIndex != path.LastIndexOf('/'))
            throw new ArgumentException("Invalid submission link.");

        string type = string.Empty;
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
        return [new Mod()
                {
                    Id = submission.Id,
                    Type = type,
                    Name = submission.Name,
                    Likes = submission.Likes,
                    Author = submission.Author.Name
                }];
    }

    public async Task<IReadOnlyList<Mod>> GetTopSubmissionsAsync(Visibility defaultVisibility = Visibility.Show)
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

        if (_cache.TryGetValue(GetCacheKey(), out object? cachedVal)
            && cachedVal is IReadOnlyList<Mod> cachedMods)
            return cachedMods;

        List<TopSubmission> top = await Gamebanana.Games.Search.TopSubmissionsAsync(DeadlockGameId);
        Mod[] mods = top
            .OrderBy(static sub => GetPriority(sub.Period))
            .Where(sub => sub.Type is "Sound" or "Mod" && (int)sub.SafeForWork <= (int)defaultVisibility)
            .Select(static sub => new Mod()
            {
                Id = sub.Id,
                Type = sub.Type,
                Name = sub.Name,
                Likes = sub.Likes,
                Author = sub.Author.Name
            }).ToArray();

        _cache.Set(GetCacheKey(), mods, s_topSubmissionsCacheTime);
        return mods;
    }

    public async Task<IReadOnlyList<Mod>> GetFeaturedAsync(Visibility defaultVisibility = Visibility.Show)
    {
        static int GetPriority(FeaturingGroup group) => group switch
        {
            FeaturingGroup.Today => 0,
            FeaturingGroup.Yesterday => 1,
            FeaturingGroup.ThreeDays => 2,
            _ => int.MaxValue
        };

        string GetCacheKey() => $"featured:{defaultVisibility}";

        if (_cache.TryGetValue(GetCacheKey(), out object? cachedVal)
            && cachedVal is IReadOnlyList<Mod> cachedMods)
            return cachedMods;

        FeaturedSubmissions featured = await Gamebanana.Games.Search.FeaturedAsync(DeadlockGameId);
        Mod[] mods = featured.Submissions
            .OrderBy(static sub =>
                sub.Featurings.OrderBy(static feat => GetPriority(feat.Group))
                .ThenByDescending(static feat => feat.Date)
                .Select(static feat => GetPriority(feat.Group))
                .FirstOrDefault(int.MaxValue))
            .Where(sub => sub.Type is "Sound" or "Mod" && (int)sub.SafeForWork <= (int)defaultVisibility && sub.HasFiles)
            .Select(static sub => new Mod()
            {
                Id = sub.Id,
                Type = sub.Type,
                Name = sub.IsObsolete
                    ? "<OBSOLETE> " + sub.Name : sub.Name,
                Likes = sub.Likes,
                Author = sub.Author.Name
            }).ToArray();

        _cache.Set(GetCacheKey(), mods, s_featuredCacheTime);
        return mods;
    }

    public void Dispose() => _cache.Dispose();
}
