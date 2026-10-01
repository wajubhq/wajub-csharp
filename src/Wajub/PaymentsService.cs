namespace Wajub;

/// <summary>Payment operations.</summary>
public class PaymentsService
{
    internal BaseClient Base { get; }

    internal PaymentsService(BaseClient baseClient) => Base = baseClient;

    public async Task<Payment> CreateAsync(
        Dictionary<string, object?> parameters,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync("/payments", parameters, options, cancellationToken);
        return Payment.Parse(response);
    }

    public async Task<Payment> RetrieveAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync($"/payments/{id}", cancellationToken: cancellationToken);
        return Payment.Parse(new Dictionary<string, object?>
        {
            ["transaction"] = JsonUtils.PickResource(response, "transaction"),
        });
    }

    public Task<PagedResult> ListAsync(
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
        => Pagination.CreatePagedListAsync(Base, "/payments", "transactions", parameters, cancellationToken);

    public async Task<Payment> CancelAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.DeleteAsync($"/payments/{id}", options, cancellationToken);
        return Payment.Parse(new Dictionary<string, object?>
        {
            ["transaction"] = JsonUtils.PickResource(response, "transaction"),
        });
    }

    public async Task<Payment> ProcessAsync(
        string id,
        Dictionary<string, object?> parameters,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync($"/payments/{id}", parameters, options, cancellationToken);
        return Payment.Parse(new Dictionary<string, object?>
        {
            ["transaction"] = JsonUtils.PickResource(response, "transaction"),
        });
    }

    public async Task<Payment> ProcessSplitAsync(
        string id,
        Dictionary<string, object?> parameters,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync($"/payments/{id}/splits", parameters, options, cancellationToken);
        return Payment.Parse(new Dictionary<string, object?>
        {
            ["transaction"] = JsonUtils.PickResource(response, "transaction"),
        });
    }

    public async Task<(List<Dictionary<string, object?>> Data, Dictionary<string, object?>? Meta)> ListRefundsAsync(
        string id,
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync($"/payments/{id}/refunds", parameters, cancellationToken);
        var (data, meta) = JsonUtils.PickList(response, "refunds");
        return (data, meta);
    }
}
