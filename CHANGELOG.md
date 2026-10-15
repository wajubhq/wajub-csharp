# Changelog

All notable changes to the `Wajub` NuGet package are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
The package uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [2.1.0] — 2026-10-15

### Added
- **`Payment.AmountDecimal` and `Refund.AmountDecimal`, the amount with its
  decimals.** The API sends amounts in the major unit of the currency, so
  GHS 12.50 arrives as `12.5`. `AmountDecimal` is a `decimal` that keeps it
  digit for digit: `12.5m` for GHS 12.50, `15000m` for XAF 15,000. An amount
  sent as a string (`"12.50"`) reads the same. Put it back in a request as is,
  for a refund for instance, and it goes out with its cents.

### Deprecated
- **`Payment.Amount` and `Refund.Amount`.** They are `long` and drop the
  decimals: GHS 12.50 reads `12`. They still hold that value, so code that
  reads them compiles and behaves as before, with an obsolete warning
  (`CS0618`). Read `AmountDecimal` instead.

### Fixed
- **A decimal in a query parameter is written with a dot whatever the
  culture.** Under a culture such as `fr-FR`, `["amount_min"] = 12.5m` on a
  list went out as `12,5`. Query values are now written with the invariant
  culture. Request bodies were not affected.
- The README said amounts are passed in the smallest currency unit (cents).
  The API reads them in the major unit: send `12.50` for GHS 12.50, not `1250`.
  Zero-decimal currencies such as XAF and XOF are not affected.

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
- **The `409` for an `Idempotency-Key` still being processed is retried.** A
  call sent again after a timeout can reach the API while the first one is
  still running; the API answers `409` with `errors.idempotency_key`. The
  client now waits 2 then 4 seconds and asks again with the same key, within
  its usual 2 retries, and returns the first request's result. Every other
  `409` is still thrown at once.

### Fixed
- A missing `X-Wajub-Signature` or `X-Wajub-Timestamp` header (`null`) threw a
  `NullReferenceException`. It now throws `WebhookSignatureVerificationError`.

## [1.2.0] — 2026-10-01

Tagged, never published to NuGet. Its change ships with 2.0.0.

### Removed
- `Payments.InitializeAsync`, an alias of `Payments.CreateAsync`. Call `CreateAsync`.

## [1.1.1] — 2026-08-09

Last version published before this changelog was started.
