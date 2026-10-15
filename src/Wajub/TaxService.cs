namespace Wajub;

/// <summary>Tax operations.</summary>
public class TaxService
{
    internal BaseClient Base { get; }

    internal TaxService(BaseClient baseClient) => Base = baseClient;

    public async Task<Dictionary<string, object?>> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync("/tax/settings", cancellationToken: cancellationToken);
        return JsonUtils.PickResource(response, "tax", "settings");
    }

    public async Task<Dictionary<string, object?>> UpdateSettingsAsync(
        Dictionary<string, object?> parameters,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PutAsync("/tax/settings", parameters, options, cancellationToken);
        return JsonUtils.PickResource(response, "tax", "settings");
    }

    public async Task<(List<Dictionary<string, object?>> Data, Dictionary<string, object?>? Meta)> RatesAsync(
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync("/tax/rates", parameters, cancellationToken);
        var (data, meta) = JsonUtils.PickList(response, "rates", "tax_rates");
        return (data, meta);
    }

    public async Task<Dictionary<string, object?>> CalculateAsync(
        Dictionary<string, object?> parameters,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync("/tax/calculate", parameters, options, cancellationToken);
        return JsonUtils.PickResource(response, "tax", "calculation");
    }

    public async Task<(List<Dictionary<string, object?>> Data, Dictionary<string, object?>? Meta)> ReportsAsync(
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync("/tax/reports", parameters, cancellationToken);
        var (data, meta) = JsonUtils.PickList(response, "reports", "tax_reports");
        return (data, meta);
    }

    public async Task<(List<Dictionary<string, object?>> Data, Dictionary<string, object?>? Meta)> ListCodesAsync(
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync("/tax/codes", parameters, cancellationToken);
        var (data, meta) = JsonUtils.PickList(response, "tax_codes", "codes");
        return (data, meta);
    }

    public async Task<Dictionary<string, object?>> RetrieveCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync($"/tax/codes/{code}", cancellationToken: cancellationToken);
        return JsonUtils.PickResource(response, "tax_code", "code");
    }

    public Task<PagedResult> ListRegistrationsAsync(
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
        => Pagination.CreatePagedListAsync(Base, "/tax/registrations", "registrations", parameters, cancellationToken);

    public async Task<Dictionary<string, object?>> CreateRegistrationAsync(
        Dictionary<string, object?> parameters,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PostAsync("/tax/registrations", parameters, options, cancellationToken);
        return JsonUtils.PickResource(response, "registration", "tax_registration");
    }

    public async Task<Dictionary<string, object?>> RetrieveRegistrationAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync($"/tax/registrations/{id}", cancellationToken: cancellationToken);
        return JsonUtils.PickResource(response, "registration", "tax_registration");
    }

    public async Task<Dictionary<string, object?>> UpdateRegistrationAsync(
        string id,
        Dictionary<string, object?> parameters,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.PutAsync($"/tax/registrations/{id}", parameters, options, cancellationToken);
        return JsonUtils.PickResource(response, "registration", "tax_registration");
    }

    public async Task DeleteRegistrationAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Base.DeleteAsync($"/tax/registrations/{id}", options, cancellationToken);
    }

    public async Task<(List<Dictionary<string, object?>> Data, Dictionary<string, object?>? Meta)> JurisdictionsAsync(
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync("/tax/jurisdictions", parameters, cancellationToken);
        var (data, meta) = JsonUtils.PickList(response, "jurisdictions", "tax_jurisdictions");
        return (data, meta);
    }

    public async Task<(List<Dictionary<string, object?>> Data, Dictionary<string, object?>? Meta)> ThresholdsAsync(
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync("/tax/thresholds", parameters, cancellationToken);
        var (data, meta) = JsonUtils.PickList(response, "thresholds", "tax_thresholds");
        return (data, meta);
    }

    public async Task<(List<Dictionary<string, object?>> Data, Dictionary<string, object?>? Meta)> ThresholdAlertsAsync(
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Base.GetAsync("/tax/thresholds/alerts", parameters, cancellationToken);
        var (data, meta) = JsonUtils.PickList(response, "alerts", "threshold_alerts");
        return (data, meta);
    }
}
