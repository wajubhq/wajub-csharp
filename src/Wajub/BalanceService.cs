namespace Wajub;

/// <summary>Balance operations.</summary>
public class BalanceService
{
    internal BaseClient Base { get; }

    internal BalanceService(BaseClient baseClient) => Base = baseClient;

    public async Task<Dictionary<string, object?>> RetrieveAsync(CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync("/balance", cancellationToken: cancellationToken);
        return JsonUtils.PickResource(response, "balance", "data");
    }
}
