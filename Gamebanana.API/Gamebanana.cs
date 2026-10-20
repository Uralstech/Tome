using Gamebanana.API.Data.Game.Featured;
using Gamebanana.API.Data.Game.Search;
using Gamebanana.API.Data.Game.TopSubs;
using Gamebanana.API.Data.Submission;
using System.Collections.Specialized;
using System.Web;
using static Gamebanana.API.Gamebanana;

namespace Gamebanana.API;

public static class Gamebanana
{
    public static readonly Uri BaseUri = new("https://gamebanana.com/apiv11/");

    public static class Games
    {
        public static class Search
        {
            public static async Task<List<TopSubmission>> TopSubmissionsAsync(int game, CancellationToken token = default)
            {
                Uri uri = new(BaseUri, $"Game/{game}/TopSubs");
                return await ApiClient.GetAsync(uri, GameTopSubsJsonContext.Default.ListTopSubmission, token);
            }

            public static async Task<FeaturedSubmissions> FeaturedAsync(int game, int page = 1, CancellationToken token = default)
            {
                UriBuilder uriBuilder = new(BaseUri);
                uriBuilder.Path += "Util/List/Featured";

                NameValueCollection queryParams = HttpUtility.ParseQueryString(uriBuilder.Query);
                queryParams["_idGameRow"] = game.ToString();
                queryParams["_nPage"] = page.ToString();

                return await ApiClient.GetAsync(uriBuilder.Uri, GameFeaturedJsonContext.Default.FeaturedSubmissions, token);
            }

            public static async Task<SearchSubmissions> SearchAsync(int game, string query,
                string? sort = null, string? includeTypes = null, string? excludeTypes = null,
                string? includeTags = null, string? excludeTags = null, int page = 1, CancellationToken token = default)
            {
                UriBuilder uriBuilder = new(BaseUri);
                uriBuilder.Path += $"Game/{game}/Subfeed";

                NameValueCollection queryParams = HttpUtility.ParseQueryString(uriBuilder.Query);
                queryParams["_nPage"] = page.ToString();
                queryParams["_sName"] = query;

                if (!string.IsNullOrEmpty(sort))
                    queryParams["_sSort"] = sort;

                if (!string.IsNullOrEmpty(includeTypes))
                    queryParams["_csvModelInclusions"] = includeTypes;

                if (!string.IsNullOrEmpty(excludeTypes))
                    queryParams["_csvModelExclusions"] = excludeTypes;

                if (!string.IsNullOrEmpty(includeTags))
                    queryParams["_csvTagInclusions"] = includeTags;

                if (!string.IsNullOrEmpty(excludeTags))
                    queryParams["_csvTagExclusions"] = excludeTags;

                return await ApiClient.GetAsync(uriBuilder.Uri, GameSearchJsonContext.Default.SearchSubmissions, token);
            }
        }
    }

    public static class Submission
    {
        public static async Task<SubmissionProfile> PropertiesAsync(int submission, string type, CancellationToken token = default)
        {
            Uri uri = new(BaseUri, $"{type}/{submission}?_csvProperties=@gbprofile");
            return await ApiClient.GetAsync(uri, SubmissionJsonContext.Default.SubmissionProfile, token);
        }
    }
}
