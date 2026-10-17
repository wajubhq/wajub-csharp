namespace Wajub;

/// <summary>Event log operations.</summary>
public class EventsService
{
    internal BaseClient Base { get; }

    internal EventsService(BaseClient baseClient) => Base = baseClient;

    public Task<PagedResult> ListAsync(
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
        => Pagination.CreatePagedListAsync(Base, "/events", "events", parameters, cancellationToken);

    public async Task<Dictionary<string, object?>> RetrieveAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync($"/events/{PathSegment.Id(id)}", cancellationToken: cancellationToken);
        return JsonUtils.PickResource(response, "event");
    }

    public async Task<Dictionary<string, object?>> ResendAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync($"/events/{PathSegment.Id(id)}/resend", null, options, cancellationToken);
        return JsonUtils.PickResource(response, "event");
    }
}
