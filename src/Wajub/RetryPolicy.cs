namespace Wajub;

internal static class RetryPolicy
{
    public const int MaxRetries = 2;

    private static readonly HashSet<int> RetryableStatus = [429, 500, 502, 503, 504];

    public static bool ShouldRetryStatus(int status) => RetryableStatus.Contains(status);

    public static TimeSpan RetryDelay(int attempt, HttpResponseMessage? response)
    {
        if (response is not null)
        {
            var delay = ParseRetryAfter(response);
            if (delay > TimeSpan.Zero)
            {
                return delay;
            }
        }

        return TimeSpan.FromMilliseconds(500 * (1 << (attempt - 1)));
    }

    /// <summary>
    /// Whether the API answered the 409 it sends while a request with the same
    /// Idempotency-Key is still running (<c>errors.idempotency_key</c>). A call
    /// sent again after a timeout can arrive before the first one has finished:
    /// its answer is on the way, so the call is worth repeating with the same
    /// key. Every other 409 is a real refusal.
    /// </summary>
    public static bool IsIdempotencyKeyInProgress(int status, Dictionary<string, object?> body)
    {
        if (status != 409 || !body.TryGetValue("errors", out var errors))
        {
            return false;
        }

        return JsonUtils.AsDictionary(errors)?.ContainsKey("idempotency_key") ?? false;
    }

    /// <summary>The pause before asking again about a key still being processed: 2, 4, then 8 seconds.</summary>
    public static TimeSpan IdempotencyConflictDelay(int attempt) => TimeSpan.FromSeconds(1 << Math.Max(1, attempt));

    public static TimeSpan ParseRetryAfter(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Retry-After", out var values))
        {
            return TimeSpan.Zero;
        }

        var header = values.FirstOrDefault()?.Trim();
        if (string.IsNullOrEmpty(header))
        {
            return TimeSpan.Zero;
        }

        if (int.TryParse(header, out var seconds))
        {
            return TimeSpan.FromSeconds(seconds);
        }

        if (DateTimeOffset.TryParse(header, out var retryTime))
        {
            var delay = retryTime - DateTimeOffset.UtcNow;
            return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
        }

        return TimeSpan.Zero;
    }
}
