## ADDED Requirements

### Requirement: Public QR-code endpoint returns a signed registration's PNG

The Registrations module SHALL expose a public, unauthenticated HTTP endpoint at `GET /teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}/qr-code?signature={signature}` that returns a PNG image of a QR code for a successfully-validated registration. The response SHALL have content type `image/png` and a content-disposition that suggests the filename `qrcode.png`.

The QR code's encoded payload SHALL be the literal string `"{registrationId}:{signature}"` so that an offline scanner can later verify the signature without an additional HTTP round-trip. The QR code SHALL be generated with error-correction level Q.

#### Scenario: Successful retrieval returns a PNG
- **WHEN** an attendee with `RegistrationId` "reg-123" for event "DevConf" on team "acme" requests the endpoint with a `signature` value that is the valid HMAC-SHA256 signature of "reg-123" under the event's signing key
- **THEN** the response is `200 OK` with content type `image/png`, content-disposition `attachment; filename="qrcode.png"`, and the PNG decodes to a QR code whose payload is `"reg-123:{signature}"`

#### Scenario: Endpoint is unauthenticated
- **WHEN** an unauthenticated client makes the request with a valid signature
- **THEN** the response is `200 OK` (no auth challenge is issued)

---

### Requirement: Signature is verified before registration lookup

The endpoint SHALL verify the signature against `(registrationId, ticketedEventId)` BEFORE loading the registration, so that the endpoint cannot be used to enumerate valid registration IDs. Verification SHALL use a constant-time comparison.

The order of checks SHALL be:

1. Resolve `teamId` from `teamSlug` (404 on unknown team slug).
2. Resolve `ticketedEventId` from `(teamId, eventSlug)` (404 on unknown event slug).
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

#### Scenario: Unknown team or event slug is rejected before signature checking
- **WHEN** the endpoint is called for `teamSlug` "ghost" or `eventSlug` "missing"
- **THEN** the response is `404 Not Found` and no signing key is loaded

#### Scenario: Comparison uses a timing-safe primitive
- **WHEN** signatures are compared during verification
- **THEN** the implementation uses a fixed-time comparison (e.g., `CryptographicOperations.FixedTimeEquals`) so per-request timing does not leak how many bytes matched

---

### Requirement: Signature is HMAC-SHA256 over the registration ID, scoped per event

A signature SHALL be the URL-safe Base64 encoding (with `+` → `-`, `/` → `_`, `=` padding stripped) of the HMAC-SHA256 of the registration ID's canonical lowercase hex string under the per-event signing key.

The signing key SHALL be the `SigningKey` of the `TicketedEvent` identified by `(teamSlug, eventSlug)` in the request path. A signature produced for one event SHALL NOT validate against any other event, even for the same registration ID.

#### Scenario: Signature format is URL-safe Base64
- **WHEN** the system signs `RegistrationId` "reg-123" for event "DevConf"
- **THEN** the resulting signature contains only characters from `[A-Za-z0-9_-]` and no `=` padding

#### Scenario: Signature does not validate cross-event
- **WHEN** a signature was produced for `RegistrationId` "reg-123" under event "DevConf"'s signing key, and the endpoint is called for the same `registrationId` but for a different event "OtherConf" that has the same `registrationId` in its registrations
- **THEN** the response is `403 Forbidden`

#### Scenario: Signature does not validate cross-registration
- **WHEN** a signature was produced for `RegistrationId` "reg-123" under event "DevConf"'s signing key, and the endpoint is called for `RegistrationId` "reg-999" with that same signature on event "DevConf"
- **THEN** the response is `403 Forbidden`

---

### Requirement: Cancelled registrations still resolve (no revocation in this capability)

The endpoint SHALL produce a QR code for any registration whose signature validates, regardless of the registration's current `Status`. Revocation of QR codes is not part of this capability; check-in tooling that consumes the QR code is responsible for inspecting the registration's `Status` and rejecting cancelled or otherwise-ineligible registrations.

#### Scenario: Cancelled registration still returns a PNG
- **WHEN** the endpoint is called for a registration whose `Status` is `Cancelled` with a valid signature
- **THEN** the response is `200 OK` with the PNG body

---

### Requirement: Signing key is internal and never returned by any read API

The per-event signing key SHALL NOT appear in any public or admin DTO, list endpoint, detail endpoint, or audit log entry. The signing key is read by the signing service only when verifying or producing a signature, and it SHALL NOT be logged in plaintext.

#### Scenario: Event detail does not expose the signing key
- **WHEN** an admin retrieves event details (`GET /admin/teams/{teamSlug}/events/{eventSlug}`)
- **THEN** the response body contains no `signingKey` field and no value derived from it

#### Scenario: Logs do not contain the signing key
- **WHEN** the signing service signs or verifies a signature
- **THEN** no log entry written by the service contains the raw signing key value
