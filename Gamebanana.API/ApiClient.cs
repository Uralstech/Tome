using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;

namespace GamebananaAPI;

internal static class ApiClient
{
    private static readonly HttpClient s_client = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    internal static async Task<T> GetAsync<T>(Uri path, JsonTypeInfo<T> json, CancellationToken token = default)
    {
        using HttpResponseMessage response = await s_client.GetAsync(path,
            HttpCompletionOption.ResponseHeadersRead, token);

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(token);
            throw new HttpRequestException($"An HTTP error occurred (code {response.StatusCode}):\n{body}", null, response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync(json, token)
            ?? throw new InvalidOperationException("The API returned an empty response");
    }
}
