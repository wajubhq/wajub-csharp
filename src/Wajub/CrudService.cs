namespace Wajub;

/// <summary>Generic CRUD service for Wajub resources.</summary>
public class CrudService
{
    internal BaseClient Base { get; }

    private readonly string _path;

    private readonly string _singular;

    private readonly string _plural;

    internal CrudService(BaseClient baseClient, string path, string singular, string plural)
    {
        Base = baseClient;
        _path = path;
        _singular = singular;
        _plural = plural;
    }

    public async Task<Dictionary<string, object?>> CreateAsync(
        Dictionary<string, object?> parameters,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync($"/{_path}", parameters, options, cancellationToken);
        return JsonUtils.PickResource(response, _singular);
    }

    public async Task<Dictionary<string, object?>> RetrieveAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync($"/{_path}/{id}", cancellationToken: cancellationToken);
        return JsonUtils.PickResource(response, _singular);
    }

    public Task<PagedResult> ListAsync(
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return Pagination.CreatePagedListAsync(Base, $"/{_path}", _plural, parameters, cancellationToken);
    }

    public async Task<Dictionary<string, object?>> UpdateAsync(
        string id,
        Dictionary<string, object?> parameters,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PutAsync($"/{_path}/{id}", parameters, options, cancellationToken);
        return JsonUtils.PickResource(response, _singular);
    }

    public async Task DeleteAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Base.DeleteAsync($"/{_path}/{id}", options, cancellationToken);
    }
}
