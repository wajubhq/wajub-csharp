namespace Wajub;

/// <summary>Shield fraud-prevention operations.</summary>
public class ShieldService
{
    internal BaseClient Base { get; }

    internal ShieldService(BaseClient baseClient) => Base = baseClient;

    public async Task<Dictionary<string, object?>> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync("/shield/settings", cancellationToken: cancellationToken);
        return JsonUtils.PickResource(response, "shield", "settings");
    }

    public async Task<Dictionary<string, object?>> UpdateSettingsAsync(
        Dictionary<string, object?> parameters,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PutAsync("/shield/settings", parameters, options, cancellationToken);
        return JsonUtils.PickResource(response, "shield", "settings");
    }

    public async Task<Dictionary<string, object?>> StatsAsync(
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync("/shield/stats", parameters, cancellationToken);
        return JsonUtils.PickResource(response, "shield", "stats");
    }

    public async Task<(List<Dictionary<string, object?>> Data, Dictionary<string, object?>? Meta)> ListBlocklistAsync(
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync("/shield/blocklist", parameters, cancellationToken);
        var (data, meta) = JsonUtils.PickList(response, "blocklist", "entries");
        return (data, meta);
    }

    public async Task<Dictionary<string, object?>> AddToBlocklistAsync(
        Dictionary<string, object?> parameters,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync("/shield/blocklist", parameters, options, cancellationToken);
        return JsonUtils.PickResource(response, "blocklist", "entry");
    }

    public async Task RemoveFromBlocklistAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Base.DeleteAsync($"/shield/blocklist/{PathSegment.Id(id)}", options, cancellationToken);
    }
}
