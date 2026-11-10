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

using GamebananaAPI.Data;
using System.Net;
using System.Net.Http.Json;

namespace GamebananaAPI;

internal static class ApiClient
{
    private static readonly HttpClient s_client = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    internal static async Task<T> GetAsync<T>(Uri path, CancellationToken token = default, bool _isRetry = false)
    {
        using HttpResponseMessage response = await s_client.GetAsync(path,
            HttpCompletionOption.ResponseHeadersRead, token);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>(GamebananaJson.Default.Options, token)
                ?? throw new InvalidOperationException("The API returned an empty response");
        }

        if (response.StatusCode is not (HttpStatusCode.NotFound or HttpStatusCode.Forbidden) && !_isRetry)
        {
            await Task.Delay(500);
            return await GetAsync<T>(path, token, _isRetry: true);
        }

        string body = await response.Content.ReadAsStringAsync(token);
        throw new HttpRequestException($"A HTTP error occurred (code {response.StatusCode}):\n{body}", null, response.StatusCode);
    }

    internal static async Task<string?> DownloadFileAsync(Uri uri, string outputPath, Action<long>? onProgress = null, CancellationToken token = default, bool _isRetry = false)
    {
        using HttpResponseMessage response = await s_client.GetAsync(uri,
            HttpCompletionOption.ResponseHeadersRead, token);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode is not (HttpStatusCode.NotFound or HttpStatusCode.Forbidden) && !_isRetry)
            {
                await Task.Delay(500);
                return await DownloadFileAsync(uri, outputPath, onProgress, token, _isRetry: true);
            }

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
