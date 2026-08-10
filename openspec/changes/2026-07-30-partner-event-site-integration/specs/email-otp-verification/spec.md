## MODIFIED Requirements

### Requirement: Attendee can request an OTP code for email verification
The system SHALL expose an API-key-protected public endpoint `POST /api/events/{eventSlug}/otp/request` that accepts an email address and issues a 6-digit one-time password (OTP) delivered to that address. The endpoint SHALL derive `TeamId` from the authenticated API-key principal and use `{eventSlug}` from the route to resolve the internal event ID within that team scope. The 6-digit code SHALL be generated using a cryptographically secure random number generator (CSPRNG). The OTP SHALL be stored as a SHA-256 hash alongside the SHA-256 hash of the email (lowercased), the resolved event scope, an expiry of 10 minutes from issuance, and a failed-attempts counter initialised to zero. Requesting a new OTP for the same email+event SHALL invalidate (mark as superseded) all previous unexpired codes for that email+event. Rate-limit and supersede lookups SHALL hash the lowercased email so mixed-case addresses are treated as the same recipient.

If the event has a configured allowed email domain and the requested email address does not match it, the system SHALL reject the request with HTTP 400 Bad Request and SHALL NOT issue an OTP or count the request against the rate limit. This domain check is based solely on public per-event configuration and does not depend on whether the address has a registration, so it does not enable email-address enumeration.

The system SHALL reject requests where more than 3 unexpired (or recently expired but still within the 10-minute window) OTP codes have already been issued for the same email+event combination, returning HTTP 429 Too Many Requests. The endpoint SHALL return HTTP 202 Accepted regardless of whether the email address has a current registration, to avoid email-address enumeration.

OTP emails SHALL be delivered via the platform email infrastructure (not the per-event SMTP); delivery is asynchronous via the outbox. The system SHALL NOT expose the generated OTP code in the HTTP response.

#### Scenario: SC001 Successful OTP request returns 202
- **WHEN** an attendee posts `{"email": "dave@example.com"}` to `POST /api/events/{eventSlug}/otp/request` for an existing event using a valid API key for the event's team
- **THEN** the response is HTTP 202 Accepted, an OTP code is stored (hashed) for "dave@example.com" on that event, and an OTP delivery email is queued via the outbox

#### Scenario: SC002 Unknown email returns 202 (no enumeration)
- **WHEN** an attendee posts an email address that has no existing registration for the event
- **THEN** the response is still HTTP 202 Accepted and an OTP email is still queued (OTP can be used for a fresh registration)

#### Scenario: SC003 New request supersedes previous pending code
- **WHEN** an attendee posts a second OTP request for the same email+event while a previous code is still unexpired
- **THEN** the previous code is invalidated, a new code is stored, and the response is HTTP 202

#### Scenario: SC004 Rate limit exceeded returns 429
- **WHEN** an attendee has already issued 3 OTP requests for the same email+event within 10 minutes
- **THEN** the response is HTTP 429 Too Many Requests and no new OTP code is issued

#### Scenario: SC005 Unknown event returns 404
- **WHEN** an attendee posts an OTP request for an event slug that does not exist for the API key's team
- **THEN** the response is HTTP 404 Not Found

#### Scenario: Disallowed email domain returns 400
- **WHEN** the event restricts registration to a specific email domain and the attendee posts an email address on a different domain
- **THEN** the response is HTTP 400 Bad Request and no OTP code is issued or counted against the rate limit

#### Scenario: Allowed email domain returns 202
- **WHEN** the event restricts registration to a specific email domain and the attendee posts an email address on that domain
- **THEN** the response is HTTP 202 Accepted and an OTP code is issued

#### Scenario: Unrestricted event accepts any domain
- **WHEN** the event has no allowed email domain configured
- **THEN** an OTP request for any valid email address returns HTTP 202 Accepted
