namespace Wajub;

/// <summary>Invoice operations.</summary>
public class InvoicesService : CrudService
{
    internal InvoicesService(BaseClient baseClient)
        : base(baseClient, "invoices", "invoice", "invoices")
    {
    }

    public async Task<Dictionary<string, object?>> SendAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync($"/invoices/{PathSegment.Id(id)}/send", null, options, cancellationToken);
        return JsonUtils.PickResource(response, "invoice");
    }

    public async Task<Dictionary<string, object?>> MarkPaidAsync(
        string id,
        Dictionary<string, object?>? parameters = null,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync($"/invoices/{PathSegment.Id(id)}/mark-paid", parameters, options, cancellationToken);
        return JsonUtils.PickResource(response, "invoice");
    }

    public async Task<Dictionary<string, object?>> CancelAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync($"/invoices/{PathSegment.Id(id)}/cancel", null, options, cancellationToken);
        return JsonUtils.PickResource(response, "invoice");
    }
}
