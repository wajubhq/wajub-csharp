namespace Wajub;

/// <summary>Configures a single API request.</summary>
public class RequestOptions
{
    public string? IdempotencyKey { get; set; }

    public Dictionary<string, string>? Headers { get; set; }

    public string? Sync { get; set; }
}
