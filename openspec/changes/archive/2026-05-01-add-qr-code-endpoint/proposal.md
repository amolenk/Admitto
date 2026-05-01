## Why

Attendees need a scannable QR code at check-in that uniquely identifies their registration, and email templates need a stable URL to embed that code. Today the new module-based architecture has no QR code endpoint and no signing primitive to make registration-bound URLs tamper-proof. The legacy `Admitto.Application` code already contains a working QR code generator (QRCoder) and HMAC-SHA256 signing service tied to a per-event key — we can lift those patterns into the Registrations module rather than designing from scratch.

## What Changes

- Add a public HTTP endpoint that returns a PNG QR code for a registration: `GET /teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}/qr-code?signature=...`. The endpoint validates the signature against the registration ID before generating the image and rejects invalid or missing signatures with `403`.
- Encode `{registrationId}:{signature}` as the QR code payload (matching the legacy convention so existing scanners/check-in code can be reused later).
- Introduce a generic `ISigningService` in `Admitto.Module.Shared` that produces and validates URL-safe HMAC-SHA256 signatures over an arbitrary byte payload using a caller-supplied key. Lift the `TimingSafeEquals` and URL-safe Base64 patterns from the legacy `Admitto.Application/Common/Cryptography/SigningService.cs`. Placement in Shared lets the upcoming Email-module verification-token endpoints adopt the same primitive without taking a Registrations dependency.
- Add `IEventSigningKeyProvider` in Shared (contract) with the implementation in Registrations (since `TicketedEvent` owns the key) so any module can ask for an event's signing key without crossing module DbContexts. The provider caches keys in `IMemoryCache`.
- Add a thin Registrations-side helper that composes the two for the registration-id payload used by the QR-code endpoint. Email will add its own helper for verification tokens when that change ships.
- Add a `SigningKey` field to the `TicketedEvent` aggregate. The key is generated when the event is created (cryptographically random, sufficient entropy for HMAC-SHA256) and is never exposed through public APIs or DTOs. Existing events created before this change get a key assigned via the schema migration's data step.
- Add `QRCoder` as a NuGet dependency on the Registrations module.
- No CLI command. Per `AGENTS.md` the CLI is legacy.
- No Admin UI surface in this change. Email template integration (signed URL composition for outgoing mail) is out of scope and will be addressed when the Email module's templating pipeline is wired up to the new Registrations facade.

## Capabilities

### New Capabilities
- `attendee-qr-code`: Public QR code retrieval for a registration, including the signing/verification rules that protect the URL.

### Modified Capabilities
- `event-management`: `TicketedEvent` gains an internal per-event `SigningKey` generated at event creation, used by signing-dependent capabilities.

## Impact

- **New code (Shared)**: `Admitto.Module.Shared/Application/Cryptography/ISigningService.cs` + implementation, `IEventSigningKeyProvider.cs` (contract).
- **New code (Registrations)**: `Application/Common/Cryptography/EventSigningKeyProvider.cs` (implements the Shared contract against `TicketedEvent.SigningKey`), `Application/Common/Cryptography/RegistrationSigner.cs` (payload-shaping helper), `Application/UseCases/Registrations/GetQRCode/PublicApi/GetQRCodeHttpEndpoint.cs`, endpoint registration entry.
- **Domain**: `TicketedEvent` aggregate gains a `SigningKey` private field + initialization in `Create`. EF entity configuration and one schema migration to add the column and backfill keys for existing rows.
- **Dependencies**: `QRCoder` (NuGet) added to the Registrations module project.
- **Public API surface**: One new public, unauthenticated `GET` endpoint returning `image/png`. Cacheable per `(registrationId, signature)` but content does not need to be cached server-side — generation is cheap.
- **Security**: HMAC verification runs before the registration is loaded; missing or invalid signatures fail with `403` and a generic error message so the endpoint cannot be used as a registration-existence oracle. The signing key never leaves the server and is not returned by any read API.
- **Legacy code**: The legacy `GetQRCodeEndpoint` and `SigningService` under `src/Admitto.Application/` are not modified by this change; they remain in the legacy project until removed in a separate cleanup.
