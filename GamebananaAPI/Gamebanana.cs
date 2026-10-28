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

using GamebananaAPI.Data.Categories;
using GamebananaAPI.Data.Game.Featured;
using GamebananaAPI.Data.Game.Search;
using GamebananaAPI.Data.Game.TopSubs;
using GamebananaAPI.Data.Submission;
using System.Collections.Specialized;
using System.Web;

namespace GamebananaAPI;

public static class Gamebanana
{
    public static readonly Uri BaseUri = new("https://gamebanana.com/apiv11/");

    public static class Games
    {
        public static class Category
        {
            public static async Task<List<TopLevelCategoryDefinition>> GetCategoryDefinitionsAsync(int game, CancellationToken token = default)
            {
                Uri uri = new(BaseUri, $"Game/{game}/CategoryTree");
                return await ApiClient.GetAsync(uri, CategoryJsonContext.Default.ListTopLevelCategoryDefinition, token);
            }
        }

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

                uriBuilder.Query = queryParams.ToString();

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

                uriBuilder.Query = queryParams.ToString();

                return await ApiClient.GetAsync(uriBuilder.Uri, GameSearchJsonContext.Default.SearchSubmissions, token);
            }
        }
    }

    public static class Submission
    {
        public static async Task<SubmissionProfile> PropertiesAsync(int submission, string type, CancellationToken token = default)
        {
            Uri uri = new(BaseUri, $"{type}/{submission}/ProfilePage");
            return await ApiClient.GetAsync(uri, SubmissionJsonContext.Default.SubmissionProfile, token);
        }

        public static Task<string?> DownloadFileAsync(Uri uri, string outputPath, Action<long>? onProgress = null, CancellationToken token = default) =>
            ApiClient.DownloadFileAsync(uri, outputPath, onProgress, token);
    }

    public static class Category
    {
        public static async Task<List<CategoryDefinition>> GetCategoryDefinitionsAsync(string categoryType, int game, CancellationToken token = default)
        {
            Uri uri = new(BaseUri, $"Util/{categoryType}/NestedStructure?_idGameRow={game}");
            return await ApiClient.GetAsync(uri, CategoryJsonContext.Default.ListCategoryDefinition, token);
        }
    }
}
