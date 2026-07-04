## MODIFIED Requirements

### Requirement: Public QR-code endpoint returns a registration ID PNG

The Registrations module SHALL expose an anonymous Public API HTTP endpoint at `GET /e/{publicSlug}/qr-code/{registrationId}` that returns a PNG image of a registration QR code. The endpoint SHALL resolve the ticketed event by `{publicSlug}` and SHALL use `{registrationId}` from the URL path. The response SHALL have content type `image/png` and a content-disposition that suggests the filename `qrcode.png`.

The QR code's encoded payload SHALL be the literal registration ID string. The QR code SHALL be generated with error-correction level Q.

#### Scenario: Successful retrieval returns a PNG

- **WHEN** an attendee with `RegistrationId` `11111111-1111-1111-1111-111111111111` requests `GET /e/azure-fest-2026/qr-code/11111111-1111-1111-1111-111111111111` for an existing registration on the event with public slug `azure-fest-2026`
- **THEN** the response is `200 OK` with content type `image/png`, content-disposition `attachment; filename="qrcode.png"`, and the PNG decodes to a QR code whose payload is `11111111-1111-1111-1111-111111111111`

#### Scenario: Endpoint does not require API key

- **WHEN** an unauthenticated client makes the request without `X-Api-Key`
- **THEN** the request can proceed to event and registration lookup

---

### Requirement: QR-code retrieval is scoped by public slug and registration ID

The endpoint SHALL resolve the event by `TicketedEvent.PublicSlug`, then load the registration by `(eventId, registrationId)`. A registration ID SHALL be treated as an attendee-held bearer secret; no additional QR-code signature or API key SHALL be required.

The order of checks SHALL be:

1. Resolve `ticketedEventId` from `{publicSlug}` (404 on unknown public slug).
2. Load the registration; reject with 404 if it does not exist or does not belong to the resolved event.
3. Generate and return the PNG.

#### Scenario: Unknown registration is rejected

- **WHEN** the endpoint is called with a `registrationId` that does not exist for the resolved event
- **THEN** the response is `404 Not Found`

#### Scenario: Unknown event is rejected before registration lookup

- **WHEN** the endpoint is called for a public slug that does not resolve to an event
- **THEN** the response is `404 Not Found` and no registration lookup is performed

#### Scenario: Signature parameter is not required

- **WHEN** the endpoint is called without a `signature` query parameter for an existing registration under the resolved event
- **THEN** the response is `200 OK` with the PNG body

---

### Requirement: Cancelled registrations still resolve (no revocation in this capability)

The endpoint SHALL produce a QR code for any registration, regardless of the registration's current `Status`. Revocation of QR codes is not part of this capability; check-in tooling that consumes the QR code is responsible for inspecting the registration's `Status` and rejecting cancelled or otherwise-ineligible registrations.

#### Scenario: Cancelled registration still returns a PNG

- **WHEN** the endpoint is called for a registration whose `Status` is `Cancelled`
- **THEN** the response is `200 OK` with the PNG body

## REMOVED Requirements

### Requirement: API-key-protected QR-code endpoint

**Reason**: QR-code images must be embeddable in attendee emails and accessible by mail clients without an API key. The API-key-protected route is part of the Partner API, not the attendee-facing Public API.

**Migration**: Use `GET /e/{publicSlug}/qr-code/{registrationId}` instead of `GET /api/events/{eventId}/registrations/{registrationId}/qr-code`.

#### Scenario: Old Partner API QR-code route is not exposed

- **WHEN** a client requests `GET /api/events/{eventId}/registrations/{registrationId}/qr-code`
- **THEN** the system does not serve the QR-code image from that route
