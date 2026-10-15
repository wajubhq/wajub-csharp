namespace Wajub;

/// <summary>Identity verification operations.</summary>
public class IdentityService
{
    internal BaseClient Base { get; }

    internal IdentityService(BaseClient baseClient) => Base = baseClient;

    public async Task<Dictionary<string, object?>> ResolveAsync(
        Dictionary<string, object?> parameters,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync("/identity/resolve", parameters, options, cancellationToken);
        return JsonUtils.PickResource(response, "identity");
    }

    public async Task<Dictionary<string, object?>> ValidateAsync(
        Dictionary<string, object?> parameters,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync("/identity/validate", parameters, options, cancellationToken);
        return JsonUtils.PickResource(response, "identity");
    }
}
