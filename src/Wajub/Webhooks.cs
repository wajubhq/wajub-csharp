using System.Text.Json;

namespace Wajub;

/// <summary>Verifies inbound webhook signatures.</summary>
public class Webhooks
{
    public string Secret { get; set; } = string.Empty;

    private static readonly TimeSpan DefaultWebhookTolerance = TimeSpan.FromSeconds(300);

    /// <summary>Verifies X-Wajub-Signature and parses the event payload.</summary>
    public Dictionary<string, object?> ConstructEvent(
        ReadOnlySpan<byte> payload,
        string signature,
        string timestamp,
        TimeSpan tolerance = default)
    {
        if (string.IsNullOrEmpty(Secret))
        {
            throw new InvalidOperationException("wajub: webhook secret is required for ConstructEvent");
        }

        if (!signature.StartsWith("v1=", StringComparison.Ordinal))
        {
            throw new WebhookSignatureVerificationError("Invalid webhook signature format. Expected v1={hash}.");
        }

        var body = System.Text.Encoding.UTF8.GetString(payload);
        var expected = Crypto.HmacSha256(Secret, $"{timestamp}.{body}");
        var received = signature["v1=".Length..];

        if (!Crypto.TimingSafeEqual(expected, received))
        {
            throw new WebhookSignatureVerificationError("Webhook signature verification failed.");
        }

        if (!long.TryParse(timestamp, out var ts))
        {
            throw new WebhookSignatureVerificationError("Invalid webhook timestamp.");
        }

        if (tolerance <= TimeSpan.Zero)
        {
            tolerance = DefaultWebhookTolerance;
        }

        var drift = TimeSpan.FromSeconds(Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts));
        if (drift > tolerance)
        {
            throw new WebhookSignatureVerificationError(
                $"Timestamp outside tolerance zone ({(int)drift.TotalSeconds}s drift, allowed {(int)tolerance.TotalSeconds}s).");
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(body, JsonUtils.SerializerOptions)
                ?? new Dictionary<string, object?>();
        }
        catch (JsonException)
        {
            throw new WebhookSignatureVerificationError("Invalid webhook payload JSON.");
        }
    }
}
