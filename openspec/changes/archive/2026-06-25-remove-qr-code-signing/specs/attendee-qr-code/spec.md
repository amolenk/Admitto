## ADDED Requirements

### Requirement: Public QR-code endpoint returns a registration ID PNG

The Registrations module SHALL expose an API-key-protected public HTTP endpoint at `GET /api/events/{eventId}/registrations/{registrationId}/qr-code` that returns a PNG image of a registration QR code. The endpoint SHALL derive `TeamId` from the authenticated API-key principal and SHALL use `{eventId}` and `{registrationId}` from the URL path. The response SHALL have content type `image/png` and a content-disposition that suggests the filename `qrcode.png`.

The QR code's encoded payload SHALL be the literal registration ID string. The QR code SHALL be generated with error-correction level Q.

#### Scenario: Successful retrieval returns a PNG

- **WHEN** an attendee with `RegistrationId` "reg-123" requests `GET /api/events/{eventId}/registrations/reg-123/qr-code` with a valid API key for the event's team
- **THEN** the response is `200 OK` with content type `image/png`, content-disposition `attachment; filename="qrcode.png"`, and the PNG decodes to a QR code whose payload is `"reg-123"`

#### Scenario: Endpoint requires API key

- **WHEN** an unauthenticated client makes the request without `X-Api-Key`
- **THEN** the response is HTTP 401 and no registration lookup is performed

### Requirement: QR-code retrieval is scoped by API key, event, and registration ID

The endpoint SHALL authenticate the required `X-Api-Key`, resolve `TeamId` from the API-key principal, resolve the event by `(TeamId, eventId)`, and then load the registration by `(TeamId, eventId, registrationId)`. A registration ID SHALL be treated as an attendee-held bearer secret; no additional QR-code signature SHALL be required.

The order of checks SHALL be:

1. Authenticate the required `X-Api-Key` and resolve `TeamId` from the API-key principal (401 on missing, invalid, or revoked key).
2. Resolve `ticketedEventId` from `(TeamId, eventId)` (404 on unknown event for the API key's team).
3. Load the registration; reject with 404 if it does not exist or does not belong to the resolved event and team.
4. Generate and return the PNG.

#### Scenario: Unknown registration is rejected

- **WHEN** the endpoint is called with a `registrationId` that does not exist for the resolved event and API-key team
- **THEN** the response is `404 Not Found`

#### Scenario: Unknown event is rejected before registration lookup

- **WHEN** the endpoint is called for an event ID that does not exist for the API key's team
- **THEN** the response is `404 Not Found` and no registration lookup is performed

#### Scenario: Signature parameter is not required

- **WHEN** the endpoint is called without a `signature` query parameter for an existing registration under the resolved event and API-key team
- **THEN** the response is `200 OK` with the PNG body

## REMOVED Requirements

### Requirement: Public QR-code endpoint returns a signed registration's PNG

**Reason**: QR-code retrieval now uses the registration ID itself as the attendee-held bearer secret, matching self-service cancellation semantics. The additional HMAC signature does not protect a stronger boundary because possession of the registration ID already authorizes attendee self-service actions.

**Migration**: Call `GET /api/events/{eventId}/registrations/{registrationId}/qr-code` without `?signature=...`. QR payload consumers should read the registration ID directly instead of parsing `registrationId:signature`.

### Requirement: Signature is verified before registration lookup

**Reason**: The endpoint no longer accepts or verifies QR-code signatures. Registration lookup is scoped by API-key team and event before loading the registration by ID.

**Migration**: Replace signature-failure expectations with not-found behavior for unknown registrations and successful behavior for existing registrations without signatures.

### Requirement: Signature is HMAC-SHA256 over the registration ID, scoped per event

**Reason**: Per-event HMAC signatures are no longer part of the QR-code contract.

**Migration**: Remove QR-code signature generation and validation. Preserve the unrelated short-lived email-verification token mechanism.

### Requirement: Signing key is internal and never returned by any read API

**Reason**: The QR-code capability no longer needs a per-event signing key. If no other production flow uses `TicketedEvent.SigningKey`, the field and provider can be removed.

**Migration**: Remove signing-key DTO/logging assertions only after removing the underlying signing key from the model; otherwise keep general secret non-exposure checks for any retained secrets.
