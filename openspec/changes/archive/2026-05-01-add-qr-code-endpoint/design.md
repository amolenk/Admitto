## Context

Admitto attendees receive an email with links to manage their registration; on event day they need to present a scannable code that uniquely identifies their registration at check-in. The legacy `Admitto.Application` project shipped a working endpoint (`GetQRCodeEndpoint`) backed by a registration-id-aware HMAC-SHA256 `SigningService` that loads a per-event signing key. As the codebase migrates onto the module-per-bounded-context layout (see `docs/arc42/05-building-block-view.md`), QR-code support has not yet been ported to the new `Admitto.Module.Registrations`. Until it is, the new module cannot serve QR codes and downstream features (signed cancel/edit links, check-in scanning) have nothing to build on.

The Registrations module already owns `TicketedEvent` and `Registration`. It already exposes public endpoints under `/teams/{teamSlug}/events/{eventSlug}/...` (see `RegistrationsModule.MapRegistrationsPublicEndpoints`). Adding the QR-code endpoint there fits the existing layering and keeps all registration-bound URLs co-located with the aggregates that own them.

## Goals / Non-Goals

**Goals:**
- Provide a public unauthenticated endpoint that returns a PNG QR code for a registration, gated by an HMAC signature bound to that registration and ticketed event.
- Establish a reusable `ISigningService` inside the Registrations module that follows the legacy semantics (URL-safe Base64 over HMAC-SHA256, per-event key, timing-safe comparison, in-memory key cache) so future flows (cancel, edit, reconfirm) can adopt it without re-deriving the design.
- Add a per-event signing key to `TicketedEvent` and ensure it is generated automatically and never leaks through any read API.

**Non-Goals:**
- Wiring signed URLs into outbound emails. Email-template integration depends on the Email module's renderer talking to a Registrations facade for signed link composition; that work is out of scope here.
- Building the email verification token endpoints (request/verify). Those will live in the Email module and consume a shared signing primitive — see Decision 3 below for the placement we are setting up. Out of scope to implement here, but the design must not preclude it.
- Building a check-in scanner / verification UI. The endpoint produces the QR code; consumers come later.
- Adding signed cancel/edit/reconfirm endpoints. Only the QR-code endpoint is delivered. The signing service is designed to be reusable, but no other endpoint adopts it in this change.
- CLI parity. Per `AGENTS.md`, the CLI is legacy and gets no new commands.
- Any change to the legacy `Admitto.Application` QR-code endpoint or signing service. They keep working until removed in a separate cleanup.

## Decisions

### 1. Endpoint shape: `GET /teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId:guid}/qr-code?signature=...`

Returns `200 image/png` on success, `403` on missing/invalid signature, `404` on unknown team/event/registration. The path identifies the registration; the `signature` query parameter is verified against `(registrationId, ticketedEventId)` before any registration lookup runs, so the endpoint cannot be probed as an existence oracle.

**Alternatives considered:**
- Encode the signature in the path: `/registrations/{id}/{signature}/qr-code`. Rejected because path segments are URL-encoded and harder to copy from a typical email client; the legacy code also kept the signature out of the path on its newer endpoints.
- Combine `id:signature` into a single opaque token. Rejected because it makes the URL unreadable when an admin needs to debug a check-in failure; HMAC-SHA256 already prevents forgery of the separated form.

### 2. QR payload format: `"{registrationId}:{signature}"`

Matches the legacy convention. Future check-in scanners (and any handheld scanner the team already configured against the legacy app) decode the same format. The payload includes the signature so an offline scanner can verify it without an extra HTTP round trip when the verification logic ships.

### 3. Signing primitive lives in `Admitto.Module.Shared` and consumes a per-event key supplier

There are at least two upcoming consumers of HMAC signing keyed per ticketed event:

1. The QR-code endpoint (this change), in the Registrations module.
2. Email verification tokens, which more naturally live in the Email module — that module owns email-sending concerns and the verification flow needs to issue and verify tokens that gate self-registration. Putting verification-token endpoints in the Email module keeps Registrations free of email-flow surface area.

If the signing implementation lived inside Registrations, the Email module would have to take a Registrations dependency just to call into a cryptographic helper, which is the wrong shape — Email already depends on Registrations only via contracts, and the verification flow does not need any Registration aggregate state.

The signing primitive therefore goes in `Admitto.Module.Shared/Application/Cryptography/`:

- `ISigningService` — the cryptographic primitive (HMAC-SHA256 + URL-safe Base64 + timing-safe verify). It is **stateless about where keys come from**; it accepts a key as bytes/string at the call site.
- `IEventSigningKeyProvider` — a per-event key supplier interface, also in Shared, that returns the signing key for a given `TicketedEventId`. The Registrations module supplies the implementation (since it owns `TicketedEvent.SigningKey`) and registers it for DI; Shared declares the contract, so Email can depend only on the abstraction.

Concrete signing helpers used by callers (e.g., `SignRegistrationAsync(registrationId, eventId)`, `SignEmailVerificationAsync(email, eventId, expiresAt)`) compose the two: the helper asks the provider for the event's key, then asks `ISigningService` to sign or verify. This keeps payload-shape decisions (what bytes are signed, expiry encoding for verification tokens) close to the consumer instead of in a single overloaded service.

**Why not Registrations:** Forces Email → Registrations dependency for a primitive Email needs to issue tokens before any Registration exists.

**Why not Email:** Email does not own ticketed events; the per-event key is owned by Registrations.

**Why not promote later:** Verification tokens are concrete, near-term work. Building the Registrations-only version now and then refactoring it across modules would mean two migrations of the same code; the cost of putting the abstraction in Shared upfront is one extra interface.

### 4. `ISigningService` semantics (lifted from legacy, generalized)

- `string Sign(ReadOnlySpan<byte> payload, ReadOnlySpan<byte> key)` — URL-safe Base64-encoded HMAC-SHA256. Stateless and synchronous; no DB access.
- `bool IsValid(ReadOnlySpan<byte> payload, string signature, ReadOnlySpan<byte> key)` — constant-time comparison via `CryptographicOperations.FixedTimeEquals`.
- A small set of payload-shaping helpers, each in the consuming module:
  - Registrations: `SignRegistrationAsync(Guid registrationId, TicketedEventId eventId)` / `VerifyRegistrationSignatureAsync(...)` — payload is the registration id's lowercase hex string.
  - Email (future): `SignEmailVerificationAsync(EmailAddress email, TicketedEventId eventId, DateTimeOffset expiresAt)` / `VerifyEmailVerificationAsync(...)` — payload includes the email, the event id, and the expiry so a leaked token cannot be replayed past its window. The exact payload encoding is decided when that change ships; nothing in this change pins it.
- Per-event key lookup: `IEventSigningKeyProvider.GetKeyAsync(TicketedEventId)` returns the raw key bytes. The implementation in Registrations caches in `IMemoryCache` keyed `skey:{eventId}`; no expiration is configured (a key-invalidation hook can land when rotation arrives). Email will inject the same provider abstraction.

**Alternatives considered:**
- Keep one fat `ISigningService` with overloads for `Guid`, `string`, etc. (the legacy shape). Rejected — the overloads conflate "what is being signed" with "how it is signed," which makes payload changes (e.g., adding expiry to verification tokens) risk breaking unrelated callers.
- Use ASP.NET Core's Data Protection API (`IDataProtectionProvider`). Rejected — the payload format is opaque and longer than necessary, keys are global rather than per-event, and the legacy QR codes were intentionally pinned to a per-event key so leaking one event's key cannot forge tickets for another event. The same per-event isolation argument applies to verification tokens.
- JWTs. Rejected for the same payload-size reason and because we do not need the additional claim structure.

### 5. Per-event `SigningKey` on `TicketedEvent`

A new private string column on `TicketedEvent` holding 32 bytes of cryptographically random data, Base64-encoded. Generated by `TicketedEvent.Create` via `RandomNumberGenerator.GetBytes(32)` so every event ends up with one without an explicit factory call site change. The property is internal/private (no public getter exposed via DTOs, no field on read DTOs, no admin endpoint reads it back).

A schema migration adds the column as nullable, backfills existing rows in a single `UPDATE` step that generates a key in SQL via `gen_random_bytes(32)` (Postgres `pgcrypto`), then alters the column to `NOT NULL`. If `pgcrypto` is unavailable, the data step instead reads the affected rows in C# from the migration host and writes generated keys row-by-row.

**Alternatives considered:**
- Store keys in app config or a single global key. Rejected — see Decision 4 (per-event isolation prevents cross-event signature reuse).
- Derive keys from a master secret + event id (HKDF). Considered. Simpler operationally, but key rotation becomes "rotate the master secret" which touches every event simultaneously. Per-event stored keys keep rotation granular when it eventually arrives. Revisit if operational pain emerges.

### 5a. Forward-compatibility check: email verification tokens

A future Email-module change will add public endpoints to request and verify email ownership tokens before self-registration. To make sure the design we are landing now does not paint that change into a corner, sketching the expected shape:

- The Email module exposes (likely) `POST /teams/{teamSlug}/events/{eventSlug}/email-verifications` to issue a token bound to `(email, eventId, expiresAt)` and `POST /teams/{teamSlug}/events/{eventSlug}/email-verifications:verify` (or equivalent) to consume one. The token format is the verifier's choice — a candidate is `Base64(payload) + "." + signature`, where `payload` carries `email|eventId|expiresAt` and the signature is produced via `ISigningService.Sign(payload, key)` with the per-event key.
- The Email module gets the per-event key via `IEventSigningKeyProvider`, registered by Registrations against the contract declared in Shared. No DbContext crossing; no Registrations module dependency beyond the abstraction.
- The same per-event isolation argument from Decision 4 applies: a token leaked from event A cannot be replayed against event B, even for the same email address.

This change does **not** ship those endpoints. It only ships the primitive (`ISigningService`) and the per-event key provider in shapes that don't need rework when the Email change lands.

### 6. Endpoint validation order (security-relevant)

The handler runs in this order so that probing the endpoint cannot enumerate valid registrations:

1. Resolve `teamId` via `IOrganizationFacade.GetTeamIdAsync` (404 on unknown slug — slugs are public).
2. Resolve `ticketedEventId` via `ITicketedEventIdLookup` (404 on unknown event slug — also public).
3. Verify signature against `(registrationId, ticketedEventId)`. **Reject with 403 before checking that the registration exists.** A valid signature on an unknown registration is treated the same as a missing registration (404 only after this point).
4. Confirm the registration belongs to the resolved event (defence in depth — HMAC alone is enough but mismatch would indicate a copy-paste bug).
5. Generate the PNG and return it.

The signing key is loaded as part of step 3 (cached after first hit). The legacy endpoint kept the same ordering; we intentionally preserve it.

### 7. QR-code rendering: lift from legacy

Use `QRCoder.QRCodeGenerator` + `PngByteQRCode` with ECC level Q and pixel-size 20, exactly as the legacy code does. ECC level Q (≈25% recovery) is the sweet spot for printed badges that may be partly damaged. The dependency is added to `Admitto.Module.Registrations.csproj`. Generation is synchronous and CPU-cheap — measured at <5ms in the legacy code — so no caching layer is added.

## Risks / Trade-offs

- **Signing-key migration races writes.** If a `TicketedEvent` is created during the migration, the EF entity already populates `SigningKey` so the new row is fine; only pre-existing rows need backfilling. → Run the `UPDATE` step before the `ALTER COLUMN ... SET NOT NULL` and gate on `WHERE signing_key IS NULL` so it is idempotent and safe to retry.
- **Public endpoint enumeration.** A leaked signature gives anyone the QR code for that registration. → Signatures are tied to `(registrationId, eventId)`, never to a user, so they cannot be replayed against another event; per-event key isolation contains a key compromise to one event's worth of registrations.
- **No revocation today.** Cancelling a registration does not invalidate its existing signature, so a cancelled registration can still produce a QR code. → Acceptable for MVP — check-in tooling will look up the registration's `Status` separately and reject `Cancelled`. Revocation is a non-goal of this change.
- **`pgcrypto` may not be enabled in all envs.** → Fall back to the C#-side backfill path described in Decision 5; the migration code carries both branches and picks the available one.
- **In-memory key cache misses across pods.** Each API pod loads keys lazily on first request per event; no cross-pod cache invalidation. → No correctness risk because keys are immutable until rotation ships. Memory footprint is one short string per active event — negligible.
- **Future signing flows may want a string overload.** → Adding it later is non-breaking; we do not pre-build it.

## Migration Plan

1. Add `SigningKey` column to `ticketed_events` (nullable).
2. Backfill existing rows (Postgres `pgcrypto` `gen_random_bytes(32)` if available; otherwise the migration's C# `Up` method reads/writes per row).
3. `ALTER COLUMN signing_key SET NOT NULL`.
4. Deploy code that registers the new endpoint and the `ISigningService` DI binding.
5. Verify locally via Aspire (`aspire start --isolated`, `aspire wait api`) that `GET .../qr-code?signature=...` returns a PNG for a freshly-registered attendee.

Rollback: revert the deploy. The column can be dropped in a follow-up migration if rollback becomes permanent — leaving it in place is harmless.
