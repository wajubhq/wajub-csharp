namespace Wajub;

/// <summary>Dispute operations.</summary>
public class DisputesService
{
    internal BaseClient Base { get; }

    internal DisputesService(BaseClient baseClient) => Base = baseClient;

    public Task<PagedResult> ListAsync(
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
        => Pagination.CreatePagedListAsync(Base, "/disputes", "disputes", parameters, cancellationToken);

    public async Task<Dictionary<string, object?>> RetrieveAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync($"/disputes/{PathSegment.Id(id)}", cancellationToken: cancellationToken);
        return JsonUtils.PickResource(response, "dispute");
    }

    public async Task<Dictionary<string, object?>> SubmitEvidenceAsync(
        string id,
        Dictionary<string, object?> parameters,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync($"/disputes/{PathSegment.Id(id)}/submit-evidence", parameters, options, cancellationToken);
        return JsonUtils.PickResource(response, "dispute");
    }

    public async Task<Dictionary<string, object?>> AcceptAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync($"/disputes/{PathSegment.Id(id)}/accept", null, options, cancellationToken);
        return JsonUtils.PickResource(response, "dispute");
    }

    public async Task<Dictionary<string, object?>> CloseAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync($"/disputes/{PathSegment.Id(id)}/close", null, options, cancellationToken);
        return JsonUtils.PickResource(response, "dispute");
    }

    public async Task<Dictionary<string, object?>> SendMessageAsync(
        string id,
        Dictionary<string, object?> parameters,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync($"/disputes/{PathSegment.Id(id)}/messages", parameters, options, cancellationToken);
        return JsonUtils.PickResource(response, "dispute");
    }
}
