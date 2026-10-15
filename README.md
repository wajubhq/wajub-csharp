# Wajub .NET SDK

[![NuGet](https://img.shields.io/nuget/v/Wajub)](https://www.nuget.org/packages/Wajub)
[![.NET](https://img.shields.io/badge/.NET-8%2B-purple)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Official server-side SDK for the [Wajub API](https://docs.wajub.com): create payments, confirm them, pay out and verify webhooks from your .NET backend.

It runs on your server with a secret (`sk.…`, `sk_test.…`) or restricted (`rk.…`, `rk_test.…`) API key. Never expose these keys in client-side code: in the browser, use [Wajub.js](https://docs.wajub.com/libraries/components/js) with your publishable key (`pk.…`, `pk_test.…`).

## Requirements

| Requirement | Version |
|-------------|---------|
| .NET | 8.0 or later |

## Installation

```bash
dotnet add package Wajub
```

## Quick start

Amounts are in the currency's **main unit**: `12.50m` for GHS 12.50, `15000` for XAF 15,000. Read a payment's amount, decimals included, from `payment.AmountDecimal`.

```csharp
using Wajub;

var wajub = WajubClient.Create(new WajubConfig
{
    ApiKey = Environment.GetEnvironmentVariable("WAJUB_API_KEY"),
    WebhookSecret = Environment.GetEnvironmentVariable("WAJUB_WEBHOOK_SECRET"),
});

var payment = await wajub.Payments.CreateAsync(new Dictionary<string, object?>
{
    ["amount"] = 15000,
    ["currency"] = "XAF",
    ["email"] = "buyer@example.com",
    ["reference"] = "order-1042",
    ["callback"] = "https://shop.example.com/order/complete",
});

// Redirect the customer to payment.AuthorizationUrl,
// or mount payment.AuthorizationToken with Wajub.js in your page.
```

Then confirm the result on your server: handle the `payment.succeeded` webhook, or call `wajub.Payments.RetrieveAsync(payment.Id)`.

Parameters take the API's field names. Every method is asynchronous and accepts a `CancellationToken`.

## Configuration

| Property | Description |
|----------|-------------|
| `ApiKey` | Secret or restricted API key (`sk.…`, `sk_test.…`, `rk.…`, `rk_test.…`). Falls back to `WAJUB_API_KEY`. Required. |
| `WebhookSecret` | Webhook signing secret (`whsec_…`), needed only for `Webhooks.ConstructEvent()`. Falls back to `WAJUB_WEBHOOK_SECRET`. |
| `HttpClient` | Your own `HttpClient`, from `IHttpClientFactory` for instance, or to set your own timeout. |

### ASP.NET Core

Register the client once, as a singleton:

```csharp
builder.Services.AddSingleton(_ => WajubClient.Create(new WajubConfig
{
    ApiKey = builder.Configuration["Wajub:ApiKey"],
    WebhookSecret = builder.Configuration["Wajub:WebhookSecret"],
}));
```

## Resources

| Purpose | Service | Methods |
|---------|---------|---------|
| Payments | `wajub.Payments` | `CreateAsync`, `RetrieveAsync`, `ListAsync`, `CancelAsync`, `ProcessAsync`, `ProcessSplitAsync`, `RefundAsync`, `RetrieveRefundAsync`, `ListRefundsAsync` |
| Refunds | `wajub.Refunds` | `CreateAsync`, `RetrieveAsync`, `ListAsync` |
| Customers | `wajub.Customers` | `CreateAsync`, `RetrieveAsync`, `UpdateAsync`, `DeleteAsync`, `ListAsync`, `BlockAsync`, `UnblockAsync`, `ActivateAsync`, `DeactivateAsync`, `ListTaxIdsAsync`, `CreateTaxIdAsync`, `DeleteTaxIdAsync` |
| Payment links | `wajub.Links` | `CreateAsync`, `RetrieveAsync`, `UpdateAsync`, `DeleteAsync`, `ListAsync` |
| Invoices | `wajub.Invoices` | `CreateAsync`, `RetrieveAsync`, `UpdateAsync`, `DeleteAsync`, `ListAsync`, `SendAsync`, `MarkPaidAsync`, `CancelAsync` |
| Payouts | `wajub.Transfers` | `CreateAsync`, `RetrieveAsync`, `ListAsync` |
| Payout recipients | `wajub.Beneficiaries` | `CreateAsync`, `RetrieveAsync`, `UpdateAsync`, `DeleteAsync`, `ListAsync` |
| Destination checks | `wajub.Identity` | `ResolveAsync`, `ValidateAsync` |
| Balance | `wajub.Balance` | `RetrieveAsync` |
| Disputes | `wajub.Disputes` | `ListAsync`, `RetrieveAsync`, `SubmitEvidenceAsync`, `AcceptAsync`, `CloseAsync`, `SendMessageAsync` |
| Events | `wajub.Events` | `ListAsync`, `RetrieveAsync`, `ResendAsync` |
| Webhook endpoints | `wajub.WebhookEndpoints` | `CreateAsync`, `RetrieveAsync`, `UpdateAsync`, `DeleteAsync`, `ListAsync`, `RotateSecretAsync` |
| Webhook verification | `wajub.Webhooks` | `ConstructEvent` |
| Connected accounts (Sync) | `wajub.Accounts` | `CreateAsync`, `RetrieveAsync`, `UpdateAsync`, `DeleteAsync`, `ListAsync`, `RegenerateTokenAsync` |
| Tax | `wajub.Tax` | `GetSettingsAsync`, `UpdateSettingsAsync`, `RatesAsync`, `CalculateAsync`, `ReportsAsync`, `ListCodesAsync`, `RetrieveCodeAsync`, `ListRegistrationsAsync`, `CreateRegistrationAsync`, `RetrieveRegistrationAsync`, `UpdateRegistrationAsync`, `DeleteRegistrationAsync`, `JurisdictionsAsync`, `ThresholdsAsync`, `ThresholdAlertsAsync` |
| Fraud (Shield) | `wajub.Shield` | `GetSettingsAsync`, `UpdateSettingsAsync`, `StatsAsync`, `ListBlocklistAsync`, `AddToBlocklistAsync`, `RemoveFromBlocklistAsync` |
| Reference data | `wajub.Global` | `PingAsync`, `ChannelsAsync`, `CountriesAsync`, `CurrenciesAsync` |

`Payments.RefundAsync` refunds a payment, in full or for the `amount` you pass, and `Payments.RetrieveRefundAsync` reads a refund by its id. Both return a `Refund`, whose amount, decimals included, is in `refund.AmountDecimal` and its currency in `refund.Currency`:

```csharp
var refund = await wajub.Payments.RefundAsync(payment.Id, new Dictionary<string, object?>
{
    ["amount"] = 5000,
    ["reason"] = "requested_by_customer",
});
```

A refund can't be changed or deleted: `wajub.Refunds.UpdateAsync` and `wajub.Refunds.DeleteAsync` always throw a `WajubError`.

## Acting for a connected account

Pass the connected account with `Sync` on the call:

```csharp
await wajub.Payments.CreateAsync(parameters, new RequestOptions { Sync = "acct_sync_ref" });
```

## Webhooks

Pass the **raw request body** as bytes, not a bound model, with the `X-Wajub-Signature` and `X-Wajub-Timestamp` headers:

```csharp
app.MapPost("/webhooks/wajub", async (HttpRequest request, WajubClient wajub) =>
{
    using var buffer = new MemoryStream();
    await request.Body.CopyToAsync(buffer);

    Dictionary<string, object?> evt;
    try
    {
        evt = wajub.Webhooks.ConstructEvent(
            buffer.ToArray(),
            request.Headers["X-Wajub-Signature"],
            request.Headers["X-Wajub-Timestamp"]);
    }
    catch (WebhookSignatureVerificationError)
    {
        return Results.BadRequest();
    }

    if (evt["event"] as string == "payment.succeeded")
    {
        // fulfil the order
    }

    return Results.Ok();
});
```

The event is a dictionary of plain values (`string`, `long` or `double`, `bool`, `null`, a nested `Dictionary<string, object?>` or a `List<object?>`); its type is in `event`. A delivery that does not verify throws `WebhookSignatureVerificationError`. If your server's clock drifts, pass a tolerance as a `TimeSpan` fourth argument.

## Pagination

`ListAsync` returns a page; rows are `Dictionary<string, object?>`:

```csharp
var page = await wajub.Payments.ListAsync(new Dictionary<string, object?> { ["per_page"] = 50 });

foreach (var payment in page.Data)
{
    Console.WriteLine($"{payment["id"]} {payment["status"]}");
}

if (page.HasMore)
{
    var next = await page.GetNextPageAsync();
}

// Or collect the rows of every page in one list.
var all = await page.AutoPagingIterAsync();
```

## Idempotency

Temporary failures are retried for you. To make a create safe to repeat on your side too, after a restart or in a job retry, pass your own key:

```csharp
await wajub.Payments.CreateAsync(parameters, new RequestOptions { IdempotencyKey = $"order-{orderId}" });
```

## Error handling

API errors throw a subclass of `WajubError`:

```csharp
try
{
    await wajub.Payments.CreateAsync(parameters);
}
catch (InvalidRequestError e)
{
    foreach (var (field, messages) in e.FieldErrors ?? new Dictionary<string, IReadOnlyList<string>>())
    {
        Console.WriteLine($"{field}: {string.Join(" ", messages)}"); // every validation message, by field
    }
}
catch (AuthenticationError)
{
    // check your API key
}
catch (WajubError e)
{
    Console.WriteLine($"{e.Code} {e.HttpStatus} {e.Message}");
}
```

| Class | Thrown on |
|-------|-----------|
| `AuthenticationError` | `401` |
| `PermissionError` | `403` |
| `NotFoundError` | `404` |
| `InvalidRequestError` | `400`, `422` |
| `RateLimitError` | `429`, with `RetryAfter` in seconds |
| `WajubError` | Any other error status, and the parent of the classes above |
| `ApiConnectionError` | No response: network error or timeout |

Every error carries `Message`, `Code`, `HttpStatus`, `FieldErrors` (every validation message of each field), `Errors` (the first message of each field) and `Raw` (the response body).

## Testing

Use a sandbox key (`sk_test.…`): the key chooses the environment, and no real money moves. The test phone numbers and cards are on [Test scenarios](https://docs.wajub.com/developer-tools/testing/test-scenarios). To receive webhooks on your machine, forward them with the [Wajub CLI](https://docs.wajub.com/developer-tools/cli/listen).

## Documentation & support

- Documentation: [docs.wajub.com/libraries/sdks/csharp](https://docs.wajub.com/libraries/sdks/csharp)
- Report issues: [github.com/wajubhq/wajub-csharp/issues](https://github.com/wajubhq/wajub-csharp/issues)

## License

MIT — see [LICENSE](LICENSE).
