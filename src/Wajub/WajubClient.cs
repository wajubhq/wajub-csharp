namespace Wajub;

/// <summary>The Wajub server SDK entry point.</summary>
public class WajubClient : IDisposable
{
    private readonly BaseClient _baseClient;
    private bool _disposed;

    public GlobalService Global { get; }

    public PaymentsService Payments { get; }

    public CustomersService Customers { get; }

    public CrudService Refunds { get; }

    public CrudService Transfers { get; }

    public CrudService Beneficiaries { get; }

    public CrudService Links { get; }

    public BalanceService Balance { get; }

    public EventsService Events { get; }

    public AccountsService Accounts { get; }

    public WebhookEndpointsService WebhookEndpoints { get; }

    public InvoicesService Invoices { get; }

    public DisputesService Disputes { get; }

    public IdentityService Identity { get; }

    public TaxService Tax { get; }

    public ShieldService Shield { get; }

    [Obsolete(ListenService.Retired)]
    public ListenService Listen { get; }

    public Webhooks Webhooks { get; }

    private WajubClient(
        BaseClient baseClient,
        GlobalService global,
        PaymentsService payments,
        CustomersService customers,
        CrudService refunds,
        CrudService transfers,
        CrudService beneficiaries,
        CrudService links,
        BalanceService balance,
        EventsService events,
        AccountsService accounts,
        WebhookEndpointsService webhookEndpoints,
        InvoicesService invoices,
        DisputesService disputes,
        IdentityService identity,
        TaxService tax,
        ShieldService shield,
        ListenService listen,
        Webhooks webhooks)
    {
        _baseClient = baseClient;
        Global = global;
        Payments = payments;
        Customers = customers;
        Refunds = refunds;
        Transfers = transfers;
        Beneficiaries = beneficiaries;
        Links = links;
        Balance = balance;
        Events = events;
        Accounts = accounts;
        WebhookEndpoints = webhookEndpoints;
        Invoices = invoices;
        Disputes = disputes;
        Identity = identity;
        Tax = tax;
        Shield = shield;
#pragma warning disable CS0618 // Kept, obsolete, working as before.
        Listen = listen;
#pragma warning restore CS0618
        Webhooks = webhooks;
    }

    /// <summary>Creates a Wajub API client.</summary>
    public static WajubClient Create(WajubConfig config)
    {
        var key = JsonUtils.NormalizeApiKey(config.ApiKey ?? Environment.GetEnvironmentVariable("WAJUB_API_KEY") ?? string.Empty);
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("wajub: api key is required");
        }

        var baseUrl = JsonUtils.DefaultApiUrl();

        var prefix = config.IdempotencyKeyPrefix;
        if (string.IsNullOrWhiteSpace(prefix))
        {
            prefix = "wajub";
        }

        var baseClient = new BaseClient(key, baseUrl, prefix, config.HttpClient);

        var webhookSecret = config.WebhookSecret ?? Environment.GetEnvironmentVariable("WAJUB_WEBHOOK_SECRET") ?? string.Empty;

        return new WajubClient(
            baseClient,
            new GlobalService(baseClient),
            new PaymentsService(baseClient),
            new CustomersService(baseClient),
            new CrudService(baseClient, "refunds", "refund", "refunds"),
            new CrudService(baseClient, "transfers", "transfer", "transfers"),
            new CrudService(baseClient, "beneficiaries", "beneficiary", "beneficiaries"),
            new CrudService(baseClient, "links", "link", "links"),
            new BalanceService(baseClient),
            new EventsService(baseClient),
            new AccountsService(baseClient),
            new WebhookEndpointsService(baseClient),
            new InvoicesService(baseClient),
            new DisputesService(baseClient),
            new IdentityService(baseClient),
            new TaxService(baseClient),
            new ShieldService(baseClient),
            new ListenService(baseClient),
            new Webhooks { Secret = webhookSecret });
    }

    /// <summary>Disposes the internally-owned <see cref="HttpClient"/>, if the SDK created one itself.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _baseClient.Dispose();
    }
}
