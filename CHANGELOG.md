# Changelog

All notable changes to the `Wajub` NuGet package are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
The package uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.2.1] — 2026-10-12

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

Tagged, never published to NuGet. Its change ships with 1.2.1.

### Removed
- `Payments.InitializeAsync`, an alias of `Payments.CreateAsync`. Call `CreateAsync`.

## [1.1.1] — 2026-08-09

Last version published before this changelog was started.
