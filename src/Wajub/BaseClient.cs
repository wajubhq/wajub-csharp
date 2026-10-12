using System.Net;
using System.Text;

namespace Wajub;

internal sealed class BaseClient : IDisposable
{
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _idempotencyKeyPrefix;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private bool _disposed;

    public BaseClient(string apiKey, string baseUrl, string idempotencyKeyPrefix, HttpClient? httpClient)
    {
        _apiKey = JsonUtils.NormalizeApiKey(apiKey);
        _baseUrl = baseUrl.TrimEnd('/');
        _idempotencyKeyPrefix = string.IsNullOrEmpty(idempotencyKeyPrefix) ? "wajub" : idempotencyKeyPrefix;
        // Only the HttpClient we create ourselves is ours to dispose; a caller-supplied
        // HttpClient (e.g. shared/pooled by the host application) is borrowed, not owned.
        _ownsHttpClient = httpClient is null;
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    public Task<Dictionary<string, object?>> GetAsync(
        string path,
        Dictionary<string, object?>? query = null,
        CancellationToken cancellationToken = default)
    {
        return RequestAsync(HttpMethod.Get, path, null, query, null, cancellationToken);
    }

    public Task<Dictionary<string, object?>> PostAsync(
        string path,
        Dictionary<string, object?>? body,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return RequestAsync(HttpMethod.Post, path, body, null, options, cancellationToken);
    }

    public Task<Dictionary<string, object?>> PutAsync(
        string path,
        Dictionary<string, object?>? body,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return RequestAsync(HttpMethod.Put, path, body, null, options, cancellationToken);
    }

    public Task<Dictionary<string, object?>> DeleteAsync(
        string path,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return RequestAsync(HttpMethod.Delete, path, null, null, options, cancellationToken);
    }

    private async Task<Dictionary<string, object?>> RequestAsync(
        HttpMethod method,
        string path,
        Dictionary<string, object?>? body,
        Dictionary<string, object?>? query,
        RequestOptions? options,
        CancellationToken cancellationToken)
    {
        var endpoint = _baseUrl + path;
        if (query is { Count: > 0 })
        {
            var builder = new UriBuilder(endpoint);
            var pairs = query
                .Where(kvp => kvp.Value is not null)
                .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!.ToString()!)}");
            builder.Query = string.Join("&", pairs);
            endpoint = builder.Uri.ToString();
        }

        string? payload = body is null ? null : JsonUtils.SerializeBody(body);

        var idempotencyKey = options?.IdempotencyKey;
        if (string.IsNullOrEmpty(idempotencyKey)
            && method != HttpMethod.Get
            && method != HttpMethod.Delete)
        {
            idempotencyKey = JsonUtils.CreateIdempotencyKey(_idempotencyKeyPrefix);
        }

        var attempt = 0;

        while (true)
        {
            using var request = new HttpRequestMessage(method, endpoint);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            request.Headers.TryAddWithoutValidation("Authorization", _apiKey);
            request.Headers.TryAddWithoutValidation("User-Agent", $"wajub-dotnet/{VersionInfo.Version}");

            if (options?.Headers is not null)
            {
                foreach (var (key, value) in options.Headers)
                {
                    request.Headers.TryAddWithoutValidation(key, value);
                }
            }

            if (!string.IsNullOrEmpty(options?.Sync))
            {
                request.Headers.TryAddWithoutValidation("X-Sync", options.Sync);
            }

            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
            }

            if (payload is not null)
            {
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            }

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                if (attempt < RetryPolicy.MaxRetries)
                {
                    await Task.Delay(RetryPolicy.RetryDelay(attempt + 1, null), cancellationToken);
                    attempt++;
                    continue;
                }

                throw new ApiConnectionError(ex.Message);
            }

            using (response)
            {
                var data = await response.Content.ReadAsStringAsync(cancellationToken);
                var parsed = JsonUtils.DecodeJson(data);

                if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
                {
                    return parsed;
                }

                if (RetryPolicy.ShouldRetryStatus((int)response.StatusCode) && attempt < RetryPolicy.MaxRetries)
                {
                    await Task.Delay(RetryPolicy.RetryDelay(attempt + 1, response), cancellationToken);
                    attempt++;
                    continue;
                }

                // The same key still being processed: the first request's answer
                // is coming. Asked again with the same key, the API returns it.
                if (!string.IsNullOrEmpty(idempotencyKey)
                    && RetryPolicy.IsIdempotencyKeyInProgress((int)response.StatusCode, parsed)
                    && attempt < RetryPolicy.MaxRetries)
                {
                    await Task.Delay(RetryPolicy.IdempotencyConflictDelay(attempt + 1), cancellationToken);
                    attempt++;
                    continue;
                }

                var retryAfter = (int)RetryPolicy.ParseRetryAfter(response).TotalSeconds;
                throw ErrorFactory.FromResponse((int)response.StatusCode, parsed, retryAfter);
            }
        }
    }
}
