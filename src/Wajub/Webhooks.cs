using System.Text.Json;

namespace Wajub;

/// <summary>Verifies inbound webhook signatures.</summary>
public class Webhooks
{
    public string Secret { get; set; } = string.Empty;

    private static readonly TimeSpan DefaultWebhookTolerance = TimeSpan.FromSeconds(300);

    /// <summary>
    /// Verifies X-Wajub-Signature and parses the event payload. Values come back as
    /// plain .NET values: <c>string</c>, <c>long</c> or <c>double</c>, <c>bool</c>,
    /// <c>null</c>, a nested <c>Dictionary&lt;string, object?&gt;</c> or a
    /// <c>List&lt;object?&gt;</c>, so <c>evt["event"] as string</c> reads the type. A
    /// missing signature or timestamp header is a <see cref="WebhookSignatureVerificationError"/>.
    /// </summary>
    public Dictionary<string, object?> ConstructEvent(
        ReadOnlySpan<byte> payload,
        string? signature,
        string? timestamp,
        TimeSpan tolerance = default)
    {
        if (string.IsNullOrEmpty(Secret))
        {
            throw new InvalidOperationException("wajub: webhook secret is required for ConstructEvent");
        }

        if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(timestamp))
        {
            throw new WebhookSignatureVerificationError("Missing X-Wajub-Signature or X-Wajub-Timestamp header.");
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
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new WebhookSignatureVerificationError("Invalid webhook payload JSON.");
            }

            // Plain values, not JsonElement: `evt["event"] as string` was always null.
            return JsonUtils.ToPlainDictionary(document.RootElement);
        }
        catch (JsonException)
        {
            throw new WebhookSignatureVerificationError("Invalid webhook payload JSON.");
        }
    }
}
