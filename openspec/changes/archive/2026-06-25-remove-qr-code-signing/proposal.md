## Why

QR-code links currently require both a registration ID and an HMAC signature, while self-service cancellation already treats the registration ID as the attendee-held bearer secret. This adds cryptographic machinery, per-event signing keys, and extra URL parameters without a clear security boundary beyond the existing high-entropy registration ID.

## What Changes

- Remove the QR-code signature requirement from attendee QR-code retrieval.
- Treat `RegistrationId` consistently as the attendee-held bearer secret for QR-code retrieval, self-cancel, and related attendee links.
- Change QR-code image generation so the encoded payload no longer includes a signature.
- Remove unused registration-bound signing infrastructure if no remaining production flow needs it.
- Update architecture documentation that currently mandates per-event HMAC signing for registration-bound public URLs.
- **BREAKING**: The public QR-code endpoint contract no longer accepts or requires `?signature=...`, and QR payloads no longer have the `registrationId:signature` shape.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `attendee-qr-code`: QR-code retrieval and payload requirements change from signed registration IDs to registration-ID bearer-secret semantics.

## Impact

- Backend public API route contract for `GET /api/events/{eventId}/registrations/{registrationId}/qr-code`.
- QR-code generation and email context link construction in the Registrations module.
- API tests covering QR-code retrieval, invalid/missing signatures, and encoded payload shape.
- Generated Admin UI API SDK/OpenAPI artifacts after the backend contract changes.
- Documentation in `docs/arc42/08-crosscutting-concepts.md` and any related architecture references to per-event registration URL signing.
- Possible database/model cleanup for `TicketedEvent.SigningKey` and registration signing services if confirmed unused.
