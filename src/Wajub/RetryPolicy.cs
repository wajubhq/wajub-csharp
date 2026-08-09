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
