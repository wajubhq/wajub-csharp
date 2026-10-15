namespace Wajub;

/// <summary>Customer operations.</summary>
public class CustomersService : CrudService
{
    internal CustomersService(BaseClient baseClient)
        : base(baseClient, "customers", "customer", "customers")
    {
    }

    public async Task<Dictionary<string, object?>> BlockAsync(
        string id,
        Dictionary<string, object?>? parameters = null,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync($"/customers/{PathSegment.Id(id)}/block", parameters, options, cancellationToken);
        return JsonUtils.PickResource(response, "customer");
    }

    public async Task<Dictionary<string, object?>> UnblockAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync($"/customers/{PathSegment.Id(id)}/unblock", null, options, cancellationToken);
        return JsonUtils.PickResource(response, "customer");
    }

    public async Task<Dictionary<string, object?>> ActivateAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync($"/customers/{PathSegment.Id(id)}/activate", null, options, cancellationToken);
        return JsonUtils.PickResource(response, "customer");
    }

    public async Task<Dictionary<string, object?>> DeactivateAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync($"/customers/{PathSegment.Id(id)}/deactivate", null, options, cancellationToken);
        return JsonUtils.PickResource(response, "customer");
    }

    public async Task<(List<Dictionary<string, object?>> Data, Dictionary<string, object?>? Meta)> ListTaxIdsAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync($"/customers/{PathSegment.Id(customerId)}/tax_ids", cancellationToken: cancellationToken);
        var (data, meta) = JsonUtils.PickList(response, "tax_ids");
        return (data, meta);
    }

    public async Task<Dictionary<string, object?>> CreateTaxIdAsync(
        string customerId,
        Dictionary<string, object?> parameters,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync($"/customers/{PathSegment.Id(customerId)}/tax_ids", parameters, options, cancellationToken);
        return JsonUtils.PickResource(response, "tax_id");
    }

    public async Task DeleteTaxIdAsync(
        string customerId,
        string taxId,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Base.DeleteAsync($"/customers/{PathSegment.Id(customerId)}/tax_ids/{PathSegment.Id(taxId)}", options, cancellationToken);
    }
}
