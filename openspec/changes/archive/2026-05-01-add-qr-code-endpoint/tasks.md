## 1. Domain — TicketedEvent signing key

- [x] 1.1 Add a private `SigningKey` (string) field to `TicketedEvent` with a public read-only property scoped narrowly so it stays internal to the module (no exposure through DTOs or contracts)
- [x] 1.2 In `TicketedEvent.Create`, generate the signing key via `RandomNumberGenerator.GetBytes(32)` and Base64-encode it; assign it on the new aggregate before returning
- [x] 1.3 Add domain-level tests covering: created events have a non-empty key, two events created back-to-back have different keys, key length decodes to ≥ 32 bytes
- [x] 1.4 Confirm the key is not part of any existing domain event payload, integration event, or `Contracts` DTO; if any leakage exists, remove it

## 2. Persistence — schema and migration

- [x] 2.1 Add `signing_key` to `TicketedEventEntityConfiguration` (string, required, no max length needed but `HasMaxLength(64)` to match Base64 of 32 bytes ≈ 44 chars, leave headroom)
- [x] 2.2 Generate an EF Core migration via the official tooling (no hand edits): adds the column as nullable
- [x] 2.3 In the same migration, backfill existing rows with freshly-generated keys (prefer `gen_random_bytes(32)` from `pgcrypto`; fall back to a C#-side row-by-row update in the migration's `Up` method if the extension is not available)
- [x] 2.4 In the same migration, alter `signing_key` to `NOT NULL` after the backfill
- [x] 2.5 Apply migration locally against the Aspire-orchestrated Postgres and verify the column is `NOT NULL` with a value on every pre-existing row

## 3. Signing primitive (Shared) and per-event key provider

- [x] 3.1 In `Admitto.Module.Shared/Application/Cryptography/`, add `ISigningService` with stateless methods `string Sign(ReadOnlySpan<byte> payload, ReadOnlySpan<byte> key)` and `bool IsValid(ReadOnlySpan<byte> payload, string signature, ReadOnlySpan<byte> key)`, plus the implementation using HMAC-SHA256, URL-safe Base64 (`+`→`-`, `/`→`_`, strip `=`), and `CryptographicOperations.FixedTimeEquals`
- [x] 3.2 In `Admitto.Module.Shared/Application/Cryptography/`, declare the `IEventSigningKeyProvider` contract: `ValueTask<ReadOnlyMemory<byte>> GetKeyAsync(TicketedEventId eventId, CancellationToken ct)`
- [x] 3.3 Register `ISigningService` (singleton) in Shared's DI composition; do not register a default key provider there
- [x] 3.4 Unit-test `ISigningService` directly (no DB): same payload+key → same signature; different keys → different signatures; tampered/empty signatures fail `IsValid`; output contains no `+` `/` or `=` characters
- [x] 3.5 In `Admitto.Module.Registrations/Application/Common/Cryptography/`, add `EventSigningKeyProvider` implementing the Shared contract by reading `TicketedEvent.SigningKey` and caching in `IMemoryCache` keyed `skey:{eventId}`; throw the registrations module's "event not found" error if the id has no row
- [x] 3.6 Register `IEventSigningKeyProvider` in the Registrations module's DI composition; ensure `IMemoryCache` is available
- [x] 3.7 Add `RegistrationSigner` (or equivalent) in Registrations that composes provider + signing service for the registration-id payload (lowercase hex of the `Guid`); expose `SignAsync(Guid, TicketedEventId)` / `IsValidAsync(Guid, string, TicketedEventId)`
- [x] 3.8 Unit-test `RegistrationSigner`: signature differs across events for the same id, differs across ids on the same event, round-trips through `IsValidAsync`

## 4. QR-code endpoint

- [x] 4.1 Add the `QRCoder` NuGet package to `Admitto.Module.Registrations.csproj`
- [x] 4.2 Create `Application/UseCases/Registrations/GetQRCode/PublicApi/GetQRCodeHttpEndpoint.cs` mapping `GET /registrations/{registrationId:guid}/qr-code` on the public route group
- [x] 4.3 Implement the handler with the validation order from the spec: resolve team → resolve event → verify signature via `RegistrationSigner` (403 on missing/invalid before any registration lookup) → load registration (404 if missing or wrong event) → generate PNG with `PngByteQRCode`, ECC level Q, pixel-size 20
- [x] 4.4 Return the PNG via `TypedResults.File` with content type `image/png` and `qrcode.png` filename
- [x] 4.5 Wire the endpoint into `RegistrationsModule.MapRegistrationsPublicEndpoints` so it ships under `/teams/{teamSlug}/events/{eventSlug}/...`
- [x] 4.6 Confirm the endpoint requires no authentication (matches existing public registration endpoints)

## 5. Tests — endpoint behaviour

- [x] 5.1 Add an integration test (fixture/builder pattern) covering the success path: register an attendee, sign the registration id, call the endpoint, assert `200`, `image/png`, non-empty body, and that the decoded QR payload equals `"{registrationId}:{signature}"`
- [x] 5.2 Test: invalid signature → `403`, no registration row read (verified via DB query counter or interceptor)
- [x] 5.3 Test: missing `signature` query parameter → `403` with the same response shape as invalid
- [x] 5.4 Test: unknown team or event slug → `404` and signing key never loaded
- [x] 5.5 Test: valid signature on unknown registration id → `404` (after signature passes)
- [x] 5.6 Test: signature produced for event A does not validate against event B (cross-event isolation)
- [x] 5.7 Test: cancelled registration with valid signature still returns `200` PNG (revocation is out of scope for this capability)

## 6. Documentation and follow-ups

- [x] 6.1 Add a brief note in the relevant arc42 section (`08-crosscutting-concepts.md` if cryptography belongs there, otherwise the Registrations entry in `05-building-block-view.md`) describing the per-event signing key and the QR-code endpoint's verification ordering
- [x] 6.2 Verify no admin or public DTO exposes `SigningKey`; spot-check `GetTicketedEventDetails` and any list endpoints
- [x] 6.3 Run module-targeted tests for Registrations and confirm they all pass
- [ ] 6.4 Manually exercise the endpoint via Aspire: `aspire start --isolated`, `aspire wait api`, register an attendee, hit the endpoint, scan the resulting PNG to confirm payload format
