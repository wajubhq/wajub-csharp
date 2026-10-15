# Changelog

All notable changes to the `Wajub` NuGet package are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
The package uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [2.1.0] — 2026-10-15

### Added
- **`Payment.AmountDecimal` and `Refund.AmountDecimal`, the amount with its
  decimals**, as a `decimal`: `12.5m` for GHS 12.50, `15000m` for XAF 15,000.
- **`Payments.RefundAsync` and `Payments.RetrieveRefundAsync`, which return a
  `Refund`.** `RefundAsync` refunds a payment, in full or for the `amount` you
  pass; `RetrieveRefundAsync` reads a refund by its id. `Refunds.CreateAsync`,
  `RetrieveAsync` and `ListAsync` still return dictionaries.
- **`FieldErrors` on API errors, every validation message of each field.** A
  `422` can give a field several messages; `Errors` keeps only the first one.
  `e.FieldErrors["amount"]` lists them all, in the order the API gave them.
  `Errors` is unchanged.
- **`Refund.Currency`**, the currency of the refund, such as `GHS`.

### Deprecated
- **`Payment.Amount` and `Refund.Amount`.** They are `long` and drop the
  decimals: GHS 12.50 reads `12`. Code that reads them still compiles and gets
  the same value, with an obsolete warning (`CS0618`). Read `AmountDecimal`
  instead.
- **`Refunds.UpdateAsync`, `Refunds.DeleteAsync`, `Transfers.UpdateAsync` and
  `Transfers.DeleteAsync`.** The API has no endpoint to change or delete a
  refund or a payout, so these calls always throw a `WajubError` with
  `HttpStatus` `405`. Don't call them; they will be removed in the next major
  version.
- **`Listen.ConfigAsync`, `Listen.AuthAsync` and the client's `Listen`
  property.** The API no longer offers them: both calls throw a `WajubError`
  with `HttpStatus` `410`. Code that uses them still compiles, with an obsolete
  warning (`CS0618`). To receive webhooks on your machine, forward them with
  the Wajub CLI: `wajub listen`.

### Fixed
- **A decimal in a list filter is sent correctly whatever the culture.** Under
  a culture such as `fr-FR`, `["amount_min"] = 12.5m` on a list was sent as
  `12,5`.
- **Amounts are in the currency's main unit**: send `12.50m` for GHS 12.50,
  not `1250`. The README said the smallest unit (cents). Currencies without
  decimals, such as XAF and XOF, are not affected.
- **An id that can't be a Wajub id is refused before anything is sent.** An
  empty id, `.`, `..` or an id with a `/` throws an `ArgumentException` that
  names the parameter. Such an id reached another endpoint:
  `Payments.RetrieveAsync("pay_1/refunds")` read the payment's refunds. Any
  other character now stays in the id: `Customers.DeleteTaxIdAsync("cus_1?x",
  "txi_1")` sent `DELETE /customers/cus_1`. Wajub ids never contain `/`, so
  calls with real ids are not affected.

## [2.0.0] — 2026-10-12

A major version: `Webhooks.ConstructEvent` returns plain values instead of
`JsonElement`, which breaks code that read the old values (see below).

### Changed
- **`Webhooks.ConstructEvent` returns plain values.** The event used to hold
  `JsonElement` values, so `evt["event"] as string` was always `null` and a
  handler written that way answered 200 without doing anything. Values are now
  `string`, `long` or `double`, `bool`, `null`, a nested
  `Dictionary<string, object?>` or a `List<object?>`. Code that read the old
  `JsonElement` values (`((JsonElement)evt["event"]).GetString()`) must switch
  to the plain values.
- **A create sent again with the same idempotency key while the first is still
  running** now returns the first request's result instead of a `409` error.

### Fixed
- A missing `X-Wajub-Signature` or `X-Wajub-Timestamp` header (`null`) threw a
  `NullReferenceException`. It now throws `WebhookSignatureVerificationError`.

## [1.2.0] — 2026-10-01

Not published to NuGet. Its change ships with 2.0.0.

### Removed
- `Payments.InitializeAsync`, an alias of `Payments.CreateAsync`. Call `CreateAsync`.

## [1.1.1] — 2026-08-09

### Changed
- README corrections; no change to the package.
