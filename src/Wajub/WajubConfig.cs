namespace Wajub;

/// <summary>Configures a new <see cref="WajubClient"/>.</summary>
public class WajubConfig
{
    public string? ApiKey { get; set; }

    public string? WebhookSecret { get; set; }

    public string? IdempotencyKeyPrefix { get; set; }

    public HttpClient? HttpClient { get; set; }
}
