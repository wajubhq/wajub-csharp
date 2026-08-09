namespace Wajub;

/// <summary>Listen (real-time) operations.</summary>
public class ListenService
{
    internal BaseClient Base { get; }

    internal ListenService(BaseClient baseClient) => Base = baseClient;

    public async Task<Dictionary<string, object?>> ConfigAsync(CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync("/listen/config", cancellationToken: cancellationToken);
        return JsonUtils.PickResource(response, "config", "listen");
    }

    public async Task<Dictionary<string, object?>> AuthAsync(
        Dictionary<string, object?> parameters,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync("/listen/auth", parameters, options, cancellationToken);
        return JsonUtils.PickResource(response, "auth", "listen");
    }
}
