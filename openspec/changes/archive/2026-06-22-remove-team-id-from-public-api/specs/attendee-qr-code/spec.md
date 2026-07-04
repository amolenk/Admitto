## MODIFIED Requirements

### Requirement: Public QR-code endpoint returns a signed registration's PNG
The Registrations module SHALL expose an API-key-protected public HTTP endpoint at `GET /api/events/{eventId}/registrations/{registrationId}/qr-code?signature={signature}` that returns a PNG image of a successfully validated registration QR code. The endpoint SHALL derive `TeamId` from the authenticated API-key principal and SHALL use `{eventId}` and `{registrationId}` from the URL path. The response SHALL have content type `image/png` and a content-disposition that suggests the filename `qrcode.png`.

The QR code's encoded payload SHALL be the literal string `"{registrationId}:{signature}"` so that an offline scanner can later verify the signature without an additional HTTP round-trip. The QR code SHALL be generated with error-correction level Q.

#### Scenario: Successful retrieval returns a PNG
- **WHEN** an attendee with `RegistrationId` "reg-123" requests `GET /api/events/{eventId}/registrations/reg-123/qr-code?signature={signature}` with a valid API key for the event's team and a valid HMAC-SHA256 signature for that registration under the event's signing key
- **THEN** the response is `200 OK` with content type `image/png`, content-disposition `attachment; filename="qrcode.png"`, and the PNG decodes to a QR code whose payload is `"reg-123:{signature}"`

#### Scenario: Endpoint requires API key
- **WHEN** an unauthenticated client makes the request with a valid signature but without `X-Api-Key`
- **THEN** the response is HTTP 401 and no signature or registration lookup is performed

---

### Requirement: Signature is verified before registration lookup
The endpoint SHALL verify the signature against `(registrationId, ticketedEventId)` BEFORE loading the registration, so that the endpoint cannot be used to enumerate valid registration IDs. Verification SHALL use a constant-time comparison.

The order of checks SHALL be:

1. Authenticate the required `X-Api-Key` and resolve `TeamId` from the API-key principal (401 on missing, invalid, or revoked key).
2. Resolve `ticketedEventId` from `(TeamId, eventId)` (404 on unknown event for the API key's team).
3. Verify the signature against `(registrationId, ticketedEventId)` using the per-event signing key (403 on missing or invalid signature).
4. Load the registration; reject with 404 if it does not exist or does not belong to the resolved event.
5. Generate and return the PNG.

A missing `signature` query parameter SHALL be treated identically to an invalid signature.

#### Scenario: Invalid signature is rejected before any registration is read
- **WHEN** the endpoint is called with a `registrationId` that exists but a `signature` that does not validate against `(registrationId, ticketedEventId)`
- **THEN** the response is `403 Forbidden` and no `Registration` row is read from the database

#### Scenario: Missing signature parameter is rejected the same as an invalid one
- **WHEN** the endpoint is called without a `signature` query parameter
- **THEN** the response is `403 Forbidden` with the same error body as an invalid-signature response

#### Scenario: Valid signature on unknown registration is rejected at step 4 only
- **WHEN** the endpoint is called with a `registrationId` that does not exist but a signature that would validate over that id under the event's signing key
- **THEN** the response is `404 Not Found` after signature verification has already passed

#### Scenario: Unknown event is rejected before signature checking
- **WHEN** the endpoint is called for an event ID that does not exist for the API key's team
- **THEN** the response is `404 Not Found` and no signing key is loaded

#### Scenario: Comparison uses a timing-safe primitive
- **WHEN** signatures are compared during verification
- **THEN** the implementation uses a fixed-time comparison (e.g., `CryptographicOperations.FixedTimeEquals`) so per-request timing does not leak how many bytes matched
