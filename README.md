# Wajub .NET SDK

[![NuGet](https://img.shields.io/nuget/v/Wajub)](https://www.nuget.org/packages/Wajub)
[![.NET](https://img.shields.io/badge/.NET-8%2B-purple)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Official **server-side** SDK for the [Wajub merchant API](https://docs.wajub.com). Accept mobile-money and card payments across Africa with a Stripe-inspired, resource-oriented client.

Use **[Wajub.js](https://docs.wajub.com/libraries/components/js)** for embedded checkout in the browser. Use this SDK on your backend with a secret (`sk_`) or restricted (`rk_`) API key — never expose secret keys in client-side code.

## Features

- Resource-oriented API (`client.Payments`, `client.Customers`, …)
- Automatic `Idempotency-Key` on mutating requests (override per call)
- Typed errors per HTTP status (`AuthenticationError`, `RateLimitError`, …)
- Automatic retries on 429, 5xx and network errors (max 2, exponential backoff), with the same `Idempotency-Key`
- A `409` for an `Idempotency-Key` still being processed is retried with the same key (after 2 then 4 s)
- Page-based pagination with `AutoPagingIterAsync` and `GetNextPageAsync`
- Webhook signature verification (HMAC-SHA256, timestamp tolerance)

## Requirements

| Requirement | Version |
|-------------|---------|
| .NET | 8.0 or later |
| Dependencies | `HttpClient` and `System.Text.Json` (included in the runtime) |

## Installation

```bash
dotnet add package Wajub
```

## Quick start

Amounts are passed in the **smallest currency unit** (e.g. cents for EUR/USD; whole francs for XAF).

### Redirect checkout

```csharp
using Wajub;

var client = WajubClient.Create(new WajubConfig
{
    ApiKey = Environment.GetEnvironmentVariable("WAJUB_API_KEY")!,
});

var payment = await client.Payments.CreateAsync(new Dictionary<string, object?>
{
    ["amount"] = 15000,
    ["currency"] = "XAF",
    ["email"] = "buyer@example.com",
    ["callback"] = "https://shop.example.com/order/complete",
});

Console.WriteLine(payment.AuthorizationUrl);
```

### Inline / overlay (embed token)

```csharp
var embed = await client.Payments.CreateAsync(new Dictionary<string, object?>
{
    ["amount"] = 15000,
    ["currency"] = "XAF",
    ["metadata"] = new Dictionary<string, object?> { ["mode"] = "embed" },
});

// Pass to Wajub.js: embed.AuthorizationToken
```

## ASP.NET Core

Register in `Program.cs`:

```csharp
builder.Services.AddSingleton(_ => WajubClient.Create(new WajubConfig
{
    ApiKey = builder.Configuration["Wajub:ApiKey"]!,
    WebhookSecret = builder.Configuration["Wajub:WebhookSecret"],
}));
```

### Webhook endpoint

Use the **raw request body**:

```csharp
app.MapPost("/webhooks/wajub", async (HttpRequest request, WajubClient wajub) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();

    try
    {
        var eventPayload = wajub.Webhooks.ConstructEvent(
            Encoding.UTF8.GetBytes(body),
            request.Headers["X-Wajub-Signature"].ToString(),
            request.Headers["X-Wajub-Timestamp"].ToString());

        if (eventPayload.TryGetValue("type", out var type) && type?.ToString() == "payment.succeeded")
        {
            // fulfill order
        }
    }
    catch (WebhookSignatureVerificationError)
    {
        return Results.BadRequest();
    }

    return Results.Ok();
});
```

## Configuration

| Variable | Description |
|----------|-------------|
| `WAJUB_API_KEY` | Secret or restricted API key (`sk_`, `sk_test.`, `rk_`, …) |
| `WAJUB_WEBHOOK_SECRET` | Webhook signing secret (`whsec_`) for `ConstructEvent()` |

Test mode is selected by your API key prefix (`sk_test.…`), not by the API URL. Production calls always go to `https://api.wajub.com`.

```csharp
var client = WajubClient.Create(new WajubConfig
{
    ApiKey = Environment.GetEnvironmentVariable("WAJUB_API_KEY")!,
    WebhookSecret = Environment.GetEnvironmentVariable("WAJUB_WEBHOOK_SECRET"),
    IdempotencyKeyPrefix = "myshop",
});
```

## Resources (merchant API)

| Service | Methods |
|---------|---------|
| `client.Global` | `PingAsync`, `ChannelsAsync`, `CountriesAsync`, `CurrenciesAsync` |
| `client.Payments` | `CreateAsync`, `RetrieveAsync`, `ListAsync`, `CancelAsync`, `ProcessAsync`, `ProcessSplitAsync`, `ListRefundsAsync` |
| `client.Customers` | `CreateAsync`, `RetrieveAsync`, `UpdateAsync`, `DeleteAsync`, `ListAsync`, `BlockAsync`, `UnblockAsync`, `ActivateAsync`, `DeactivateAsync`, `ListTaxIdsAsync`, `CreateTaxIdAsync`, `DeleteTaxIdAsync` |
| `client.Refunds` | `CreateAsync`, `RetrieveAsync`, `ListAsync` |
| `client.Transfers` | `CreateAsync`, `RetrieveAsync`, `ListAsync` |
| `client.Beneficiaries` | `CreateAsync`, `RetrieveAsync`, `UpdateAsync`, `DeleteAsync`, `ListAsync` |
| `client.Links` | `CreateAsync`, `RetrieveAsync`, `UpdateAsync`, `DeleteAsync`, `ListAsync` |
| `client.Invoices` | `CreateAsync`, `RetrieveAsync`, `UpdateAsync`, `DeleteAsync`, `ListAsync`, `SendAsync`, `MarkPaidAsync`, `CancelAsync` |
| `client.Accounts` | `CreateAsync`, `RetrieveAsync`, `UpdateAsync`, `DeleteAsync`, `ListAsync`, `RegenerateTokenAsync` |
| `client.WebhookEndpoints` | `CreateAsync`, `RetrieveAsync`, `UpdateAsync`, `DeleteAsync`, `ListAsync`, `RotateSecretAsync` |
| `client.Balance` | `RetrieveAsync` |
| `client.Events` | `ListAsync`, `RetrieveAsync`, `ResendAsync` |
| `client.Disputes` | `ListAsync`, `RetrieveAsync`, `SubmitEvidenceAsync`, `AcceptAsync`, `CloseAsync`, `SendMessageAsync` |
| `client.Identity` | `ResolveAsync`, `ValidateAsync` |
| `client.Tax` | `GetSettingsAsync`, `UpdateSettingsAsync`, `RatesAsync`, `CalculateAsync`, `ReportsAsync`, `ListCodesAsync`, `RetrieveCodeAsync`, `ListRegistrationsAsync`, `CreateRegistrationAsync`, `RetrieveRegistrationAsync`, `UpdateRegistrationAsync`, `DeleteRegistrationAsync`, `JurisdictionsAsync`, `ThresholdsAsync`, `ThresholdAlertsAsync` |
| `client.Shield` | `GetSettingsAsync`, `UpdateSettingsAsync`, `StatsAsync`, `ListBlocklistAsync`, `AddToBlocklistAsync`, `RemoveFromBlocklistAsync` |
| `client.Listen` | `ConfigAsync`, `AuthAsync` |
| `client.Webhooks` | `ConstructEvent` (local — no HTTP) |

## Sync (Connect)

```csharp
await client.Payments.CreateAsync(parameters, new RequestOptions { Sync = "acct_sync_ref" });
```

## Webhooks

```csharp
var eventPayload = client.Webhooks.ConstructEvent(
    bodyBytes,  // byte[] — raw body, not parsed JSON
    Request.Headers["X-Wajub-Signature"],
    Request.Headers["X-Wajub-Timestamp"]);

if (eventPayload["event"] as string == "payment.succeeded")
{
    // fulfill order
}
```

During local development, use the [Wajub CLI](https://github.com/wajubhq/wajub-cli) to forward webhooks to your machine.

## Pagination

```csharp
var page = await client.Payments.ListAsync(new Dictionary<string, object?> { ["per_page"] = 50 });

var all = await page.AutoPagingIterAsync();
foreach (var payment in all)
{
    Console.WriteLine($"{payment["id"]} {payment["status"]}");
}

// Manual page control
var first = await client.Payments.ListAsync();
if (first.HasMore)
{
    var second = await first.GetNextPageAsync();
}
```

## Idempotency

POST and PUT requests automatically receive an `Idempotency-Key` header. Pass your own:

```csharp
await client.Payments.CreateAsync(parameters, new RequestOptions { IdempotencyKey = $"order-{orderId}" });
```

## Error handling

```csharp
try
{
    await client.Payments.CreateAsync(parameters);
}
catch (InvalidRequestError e)
{
    Console.WriteLine(e.Errors); // field-level validation errors
}
catch (AuthenticationError)
{
    // 401 — bad API key
}
catch (RateLimitError)
{
    // 429 — back off and retry
}
```

## Development

```bash
dotnet test
```

## Documentation & support

- Full API reference: [docs.wajub.com/libraries/sdks/csharp](https://docs.wajub.com/libraries/sdks/csharp)
- Report issues: [github.com/wajubhq/wajub-csharp/issues](https://github.com/wajubhq/wajub-csharp/issues)

## License

MIT — see [LICENSE](LICENSE).
