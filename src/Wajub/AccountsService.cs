namespace Wajub;

/// <summary>Account operations.</summary>
public class AccountsService : CrudService
{
    internal AccountsService(BaseClient baseClient)
        : base(baseClient, "accounts", "account", "accounts")
    {
    }

    public async Task<Dictionary<string, object?>> RegenerateTokenAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync($"/accounts/{id}/token", null, options, cancellationToken);
        return JsonUtils.PickResource(response, "account");
    }
}
