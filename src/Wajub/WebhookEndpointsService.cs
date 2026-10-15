namespace Wajub;

/// <summary>Webhook endpoint operations.</summary>
public class WebhookEndpointsService : CrudService
{
    internal WebhookEndpointsService(BaseClient baseClient)
        : base(baseClient, "webhooks", "endpoint", "endpoints")
    {
    }

    public async Task<Dictionary<string, object?>> RotateSecretAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync($"/webhooks/{PathSegment.Id(id)}/rotate-secret", null, options, cancellationToken);
        return JsonUtils.PickResource(response, "endpoint");
    }
}
