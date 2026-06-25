## Context

The current attendee QR-code capability requires `GET /api/events/{eventId}/registrations/{registrationId}/qr-code?signature={signature}` and encodes `"{registrationId}:{signature}"` in the PNG. The signature is an HMAC over the registration ID using a per-event `TicketedEvent.SigningKey`.

At the same time, self-service cancellation already accepts `X-Api-Key`, `eventId`, and `registrationId` without a signature. That makes the registration ID the actual attendee-held bearer secret for a stronger mutation than QR-code retrieval. Keeping a second signature on QR codes therefore adds complexity without establishing a distinct trust boundary.

The existing architecture docs explicitly mandate per-event HMAC signing for registration-bound public URLs. This change intentionally revises that cross-cutting convention for QR-code retrieval and should update the docs alongside the code.

## Goals / Non-Goals

**Goals:**

- Make QR-code retrieval use the same bearer-secret model as self-service cancellation: possession of the high-entropy `RegistrationId`, scoped by API key team and event.
- Remove signature validation and signature payload encoding from the QR-code endpoint.
- Remove obsolete signing services and per-event signing key storage when no remaining production flow uses them.
- Keep event/team scoping and API-key authentication unchanged.
- Keep cancelled-registration QR-code behavior unchanged: QR generation is not the revocation mechanism.

**Non-Goals:**

- Designing the future check-in scanner or check-in authorization model.
- Introducing revocable QR tokens or short-lived attendee links.
- Changing self-service cancellation behavior beyond consistency checks and tests if needed.
- Changing email-verification tokens or other unrelated HMAC-authenticated flows.

## Decisions

### Use `RegistrationId` as the QR bearer secret

The QR-code endpoint SHALL require only the existing route registration ID in addition to the API key and event route. `RegistrationId.New()` uses a random GUID, and existing self-service cancellation already relies on that value as a bearer secret.

Alternative considered: keep the HMAC signature for QR retrieval. This was rejected because a leaked registration ID already permits attendee self-cancel, so the signature does not protect the more sensitive operation.

### Encode only registration identity in the QR payload

The QR payload SHALL no longer include the signature. The preferred payload is the literal registration ID string unless implementation discovers an existing consumer needs event ID included as well. Server-side check-in can resolve the registration under the selected event/team and inspect registration status.

Alternative considered: encode `eventId:registrationId`. This can reduce ambiguity for scanner flows, but the current capability is only QR image retrieval and the endpoint is already event-scoped. If future check-in UX needs event self-identification, that should be designed in the check-in capability.

### Remove signing infrastructure only after proving it is unused

Implementation should remove `RegistrationSigner`, the per-event signing-key provider, `TicketedEvent.SigningKey`, and related persistence only if no production flow still references them. Email verification and internal request HMACs are separate mechanisms and must remain intact.

Alternative considered: leave `SigningKey` in the model as dormant compatibility state. This was rejected unless removal is blocked by migration risk, because the proposal's purpose is to remove unnecessary signing machinery rather than hide it.

### Update architecture documentation with the new convention

`docs/arc42/08-crosscutting-concepts.md` currently states that registration-bound public URLs are protected by per-event HMAC signatures. This must be changed to document the registration-ID bearer-secret convention for QR/self-service links, including the API-key/event/team scoping that limits lookup scope.

## Risks / Trade-offs

- Registration IDs appearing in QR images or links become the sole attendee secret for QR retrieval and self-service actions -> keep IDs high entropy, avoid exposing them in broad admin/export contexts unnecessarily, and preserve API-key/event/team scoping.
- Removing signatures drops offline HMAC validation for QR payloads -> future check-in should validate server-side or introduce a dedicated check-in token design if offline validation becomes a real requirement.
- Existing clients or emails may still include `?signature=` or `/{signature}` style links -> update link construction and generated SDKs; decide whether old event-site routes need redirects outside the API contract.
- Removing `TicketedEvent.SigningKey` requires an EF migration and migration snapshot update -> generate the migration through official EF tooling and keep rollback simple by restoring the column if needed.

## Migration Plan

1. Update the QR-code endpoint to ignore/remove the `signature` query parameter and generate payloads without signatures.
2. Update email context link construction to stop appending signatures to QR-code links.
3. Remove unused signing services/model state and generate an EF migration if `TicketedEvent.SigningKey` is removed.
4. Regenerate the OpenAPI/Admin UI SDK after the API contract changes.
5. Update architecture docs and tests.

Rollback is straightforward before data migration by reintroducing the signature parameter and validation. After dropping the signing-key column, rollback requires a reverse migration that recreates keys for existing events.

## Open Questions

- Should the QR payload be exactly `{registrationId}` or `{eventId}:{registrationId}`? The default for this change is `{registrationId}` because the capability is event-scoped and future check-in design can revisit scanner payload needs.
- Are there deployed event-site routes that currently expect `/qr-code/{registrationId}/{signature}` outside the API endpoint? If so, redirects or event-site updates may be needed outside this backend change.
