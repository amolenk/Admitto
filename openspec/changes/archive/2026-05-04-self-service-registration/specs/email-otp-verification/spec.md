## ADDED Requirements

### Requirement: Attendee can request an OTP code for email verification
The system SHALL expose a public endpoint `POST /events/{teamSlug}/{eventSlug}/otp/request` that accepts an email address and issues a 6-digit one-time password (OTP) delivered to that address. The OTP SHALL be stored as a SHA-256 hash alongside the SHA-256 hash of the email (lowercased), the event scope, an expiry of 10 minutes from issuance, and a failed-attempts counter initialised to zero. Requesting a new OTP for the same email+event SHALL invalidate (mark as superseded) all previous unexpired codes for that email+event.

The system SHALL reject requests where more than 3 unexpired (or recently expired but still within the 10-minute window) OTP codes have already been issued for the same email+event combination, returning HTTP 429 Too Many Requests. The endpoint SHALL return HTTP 202 Accepted regardless of whether the email address has a current registration, to avoid email-address enumeration.

OTP emails SHALL be delivered via the platform email infrastructure (not the per-event SMTP); delivery is asynchronous via the outbox. The system SHALL NOT expose the generated OTP code in the HTTP response.

#### Scenario: SC001 Successful OTP request returns 202
- **WHEN** an attendee posts `{"email": "dave@example.com"}` to the OTP request endpoint for an existing event
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
- **WHEN** an attendee posts an OTP request for a teamSlug/eventSlug that does not exist
- **THEN** the response is HTTP 404 Not Found

---

### Requirement: Attendee can verify an OTP code and receive a verification token
The system SHALL expose a public endpoint `POST /events/{teamSlug}/{eventSlug}/otp/verify` that accepts an email address and a 6-digit OTP code. On successful verification the system SHALL return a short-lived HMAC-signed JWT (HS256) containing `email`, `eventId`, `teamId`, and `exp` (15 minutes from issuance). The OTP code SHALL be marked as used (`UsedAt` set) and SHALL NOT be accepted again. The failed-attempts counter SHALL be incremented on each mismatch; after 5 failed attempts the code SHALL be permanently locked and return HTTP 422 Unprocessable Entity on any further attempt, even before expiry.

The system SHALL reject: expired codes (HTTP 422), already-used codes (HTTP 422), locked codes (HTTP 422), codes not found for the supplied email+event (HTTP 422). All rejection reasons SHALL return the same HTTP status and a generic `"otp invalid or expired"` message to prevent oracle attacks.

#### Scenario: SC006 Successful OTP verification returns token
- **GIVEN** an unexpired, unused OTP code for "dave@example.com" on event "devconf-2026"
- **WHEN** the attendee posts `{"email": "dave@example.com", "code": "<correct-code>"}` to the verify endpoint
- **THEN** the response is HTTP 200 with a signed JWT token containing email "dave@example.com" and the event's ID, and the OTP code is marked as used

#### Scenario: SC007 Wrong OTP code increments failed attempts
- **GIVEN** an unexpired, unused OTP code for "dave@example.com"
- **WHEN** the attendee posts an incorrect code
- **THEN** the response is HTTP 422 with reason "otp invalid or expired" and the failed-attempts counter on the code increments by 1

#### Scenario: SC008 Code locked after 5 failed attempts
- **GIVEN** an OTP code for "dave@example.com" that has already accumulated 4 failed attempts
- **WHEN** the attendee posts a 5th incorrect code
- **THEN** the code is permanently locked, the response is HTTP 422, and no further attempts are accepted even if the code has not yet expired

#### Scenario: SC009 Expired code returns 422
- **GIVEN** an OTP code for "dave@example.com" that expired 1 minute ago
- **WHEN** the attendee posts the correct code
- **THEN** the response is HTTP 422 with reason "otp invalid or expired"

#### Scenario: SC010 Already-used code returns 422
- **GIVEN** an OTP code for "dave@example.com" that was already used
- **WHEN** the attendee posts the same code again
- **THEN** the response is HTTP 422 with reason "otp invalid or expired"

#### Scenario: SC011 No code exists for email+event returns 422
- **WHEN** the attendee posts a verify request for "nobody@example.com" who has never requested an OTP for the event
- **THEN** the response is HTTP 422 with reason "otp invalid or expired"
