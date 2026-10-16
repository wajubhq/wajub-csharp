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
- **An `Idempotency-Key` in `RequestOptions.Headers` is used as the request's
  key.** Such a call to an endpoint that takes a key, such as
  `Payments.CreateAsync`, failed with a `422` (the request carried two keys)
  and now uses that key; with a different `RequestOptions.IdempotencyKey`, it
  throws `ArgumentException` before sending.
- **A blank `RequestOptions.IdempotencyKey`, or a blank `Idempotency-Key` in
  `RequestOptions.Headers`, counts as no key**: the request is protected as
  when you give none.

### Security
- **An id that is not a Wajub id is refused before anything is sent.** If an
  id you pass can come from a URL or a form, a call could act on another
  resource of your account: `Customers.DeleteAsync("cus_1/tax_ids/txi_1")`
  deleted a tax ID. An id that is empty, `.` or `..`, or contains `/`, now
  throws `ArgumentException` (`ArgumentNullException` for null). Any other
  character, such as `?` or `#`, is escaped and stays in the id. Wajub ids
  never take these forms, so valid calls are unchanged.

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
