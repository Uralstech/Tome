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
            throw new HttpRequestException($"A HTTP error occurred (code {response.StatusCode}):\n{body}", null, response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync(json, token)
            ?? throw new InvalidOperationException("The API returned an empty response");
    }

    internal static async Task<string?> DownloadFileAsync(Uri uri, string outputPath, Action<long>? onProgress = null, CancellationToken token = default)
    {
        using HttpResponseMessage response = await s_client.GetAsync(uri,
            HttpCompletionOption.ResponseHeadersRead, token);

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(token);
            throw new HttpRequestException($"A HTTP error occurred (code {response.StatusCode}):\n{body}", null, response.StatusCode);
        }

        const int BufferSize = 1024 * 1024;
        using Stream contentStream = await response.Content.ReadAsStreamAsync(token);
        using FileStream fileStream = new(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true);

        long totalRead = 0L;
        Memory<byte> buffer = new byte[BufferSize];
        bool isMoreToRead = true;

        do
        {
            int read = await contentStream.ReadAsync(buffer, token);
            if (read == 0)
            {
                isMoreToRead = false;
                break;
            }

            await fileStream.WriteAsync(buffer[..read], token);
            totalRead += read;

            onProgress?.Invoke(totalRead);
        }
        while (isMoreToRead);

        return response.Content.Headers.ContentType?.MediaType;
    }
}
