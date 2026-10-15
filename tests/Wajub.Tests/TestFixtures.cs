using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Wajub.Tests;

internal sealed class RecordedRequest
{
    public string Method { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string Authorization { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;

    public string Sync { get; set; } = string.Empty;

    public string Query { get; set; } = string.Empty;

    public List<string> IdempotencyKeyAttempts { get; } = new();

    public List<string> Bodies { get; } = new();
}

internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly List<Func<HttpRequestMessage, (int Status, object Payload)>> _responses;
    private int _call;
    public RecordedRequest Last { get; } = new();

    public MockHttpMessageHandler(IEnumerable<Func<HttpRequestMessage, (int Status, object Payload)>> responses)
    {
        _responses = responses.ToList();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Last.Method = request.Method.Method;
        Last.Path = request.RequestUri?.AbsolutePath ?? string.Empty;
        Last.Query = request.RequestUri?.Query.TrimStart('?') ?? string.Empty;

        if (request.Headers.TryGetValues("Authorization", out var authValues))
        {
            Last.Authorization = authValues.FirstOrDefault() ?? string.Empty;
        }
        else if (request.Headers.Authorization is { } auth)
        {
            // Raw API keys (sk_test.…) are parsed with the whole key as Scheme and no Parameter.
            Last.Authorization = string.IsNullOrEmpty(auth.Parameter) ? auth.Scheme : auth.Parameter;
        }

        if (request.Headers.TryGetValues("Idempotency-Key", out var idempotencyValues))
        {
            Last.IdempotencyKey = idempotencyValues.FirstOrDefault() ?? string.Empty;
        }
        else
        {
            Last.IdempotencyKey = string.Empty;
        }

        Last.IdempotencyKeyAttempts.Add(Last.IdempotencyKey);

        if (request.Headers.TryGetValues("X-Sync", out var syncValues))
        {
            Last.Sync = syncValues.FirstOrDefault() ?? string.Empty;
        }
        else
        {
            Last.Sync = string.Empty;
        }

        if (request.Content is not null)
        {
            Last.Bodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
        }

        var status = 200;
        object payload = new Dictionary<string, object?>();
        if (_call < _responses.Count)
        {
            (status, payload) = _responses[_call](request);
        }

        _call++;

        var json = JsonSerializer.Serialize(payload);
        return new HttpResponseMessage((HttpStatusCode)status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }
}

internal static class TestFixtures
{
    public const string TestApiKey = "sk_test.unit_test_key";

    public static (HttpClient Client, RecordedRequest Recorded) CreateTestClient(
        IEnumerable<Func<HttpRequestMessage, (int Status, object Payload)>> responses)
    {
        var handler = new MockHttpMessageHandler(responses);
        var httpClient = new HttpClient(handler);
        return (httpClient, handler.Last);
    }

    public static WajubClient CreateWajubClient(HttpClient httpClient)
    {
        return WajubClient.Create(new WajubConfig
        {
            ApiKey = TestApiKey,
            HttpClient = httpClient,
        });
    }

    public static string SignWebhook(string secret, string payload, long timestamp)
    {
        var mac = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{payload}"));
        return "v1=" + Convert.ToHexString(mac).ToLowerInvariant();
    }
}
