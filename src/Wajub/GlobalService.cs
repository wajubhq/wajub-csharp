namespace Wajub;

/// <summary>Global reference data and health checks.</summary>
public class GlobalService
{
    internal BaseClient Base { get; }

    internal GlobalService(BaseClient baseClient) => Base = baseClient;

    public Task<Dictionary<string, object?>> PingAsync(CancellationToken cancellationToken = default)
        => Base.GetAsync("/", cancellationToken: cancellationToken);

    public async Task<(List<Dictionary<string, object?>> Data, Dictionary<string, object?>? Meta)> ChannelsAsync(
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync("/channels", parameters, cancellationToken);
        var (data, meta) = JsonUtils.PickList(response, "channels");
        return (data, meta);
    }

    public async Task<(List<Dictionary<string, object?>> Data, Dictionary<string, object?>? Meta)> CountriesAsync(
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync("/countries", parameters, cancellationToken);
        var (data, meta) = JsonUtils.PickList(response, "countries");
        return (data, meta);
    }

    public async Task<(List<Dictionary<string, object?>> Data, Dictionary<string, object?>? Meta)> CurrenciesAsync(
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync("/currencies", parameters, cancellationToken);
        var (data, meta) = JsonUtils.PickList(response, "currencies");
        return (data, meta);
    }
}
