namespace Wajub;

/// <summary>Listen (real-time) operations.</summary>
public class ListenService
{
    internal BaseClient Base { get; }

    internal const string Retired =
        "The API no longer offers Listen: its calls fail with 410 Gone. To receive webhooks on your machine, forward them with the Wajub CLI: wajub listen.";

    internal ListenService(BaseClient baseClient) => Base = baseClient;

    [Obsolete(Retired)]
    public async Task<Dictionary<string, object?>> ConfigAsync(CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync("/listen/config", cancellationToken: cancellationToken);
        return JsonUtils.PickResource(response, "config", "listen");
    }

    [Obsolete(Retired)]
    public async Task<Dictionary<string, object?>> AuthAsync(
        Dictionary<string, object?> parameters,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync("/listen/auth", parameters, options, cancellationToken);
        return JsonUtils.PickResource(response, "auth", "listen");
    }
}
