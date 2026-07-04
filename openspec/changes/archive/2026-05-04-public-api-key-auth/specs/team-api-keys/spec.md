## ADDED Requirements

### Requirement: Team member can create an API key
A team member with appropriate admin access SHALL be able to create a named API key for their team. The system SHALL generate a cryptographically random key and return the raw key value exactly once in the creation response. The key SHALL be stored as a SHA-256 hash; the raw value SHALL NOT be retrievable after the initial response. The system SHALL also store an 8-character prefix of the raw key to aid identification.

#### Scenario: SC001 - Successful API key creation
- **WHEN** a team member sends `POST /admin/teams/{teamSlug}/api-keys` with a valid name
- **THEN** the system creates a new active API key for the team and returns 201 with `{ id, name, keyPrefix, key }` where `key` is the full raw value

#### Scenario: SC002 - API key name is required
- **WHEN** a team member sends `POST /admin/teams/{teamSlug}/api-keys` without a name
- **THEN** the system returns 422 (validation error)

#### Scenario: SC003 - API key name max length
- **WHEN** a team member sends `POST /admin/teams/{teamSlug}/api-keys` with a name exceeding 100 characters
- **THEN** the system returns 422 (validation error)

---

### Requirement: Team member can list API keys
A team member SHALL be able to retrieve a list of all API keys (active and revoked) for their team. The raw key value SHALL NOT be included in list responses. The response SHALL include `id`, `name`, `keyPrefix`, `createdAt`, `createdBy`, and `revokedAt` (null if active).

#### Scenario: SC004 - List returns all keys for the team
- **WHEN** a team member sends `GET /admin/teams/{teamSlug}/api-keys`
- **THEN** the system returns 200 with an array of API key summaries for that team only

#### Scenario: SC005 - Raw key is not exposed in list
- **WHEN** a team member lists API keys after creating one
- **THEN** the response items contain `keyPrefix` but not the full key value

---

### Requirement: Team member can revoke an API key
A team member SHALL be able to revoke an active API key by its ID. Once revoked, the key SHALL immediately cease to authenticate requests. Revoking an already-revoked key SHALL return a conflict response.

#### Scenario: SC006 - Successful revocation
- **WHEN** a team member sends `DELETE /admin/teams/{teamSlug}/api-keys/{keyId}` for an active key
- **THEN** the system marks the key as revoked and returns 204

#### Scenario: SC007 - Revoke already-revoked key
- **WHEN** a team member sends `DELETE /admin/teams/{teamSlug}/api-keys/{keyId}` for an already-revoked key
- **THEN** the system returns 409 (conflict)

#### Scenario: SC008 - Revoke key belonging to different team
- **WHEN** a team member sends `DELETE /admin/teams/{teamSlug}/api-keys/{keyId}` where the key belongs to a different team
- **THEN** the system returns 404

---

### Requirement: Public API requires a valid team API key
All public endpoints under `/api/` SHALL require a valid `X-Api-Key` header. The key SHALL be matched against the active API keys of the team identified by the `{teamSlug}` in the request path. Requests without a key, with an invalid key, or with a key belonging to a different team SHALL be rejected.

#### Scenario: SC009 - No API key provided
- **WHEN** a request is made to any public endpoint without an `X-Api-Key` header
- **THEN** the system returns 401

#### Scenario: SC010 - Invalid or unknown API key
- **WHEN** a request is made to any public endpoint with an `X-Api-Key` header containing an unrecognized value
- **THEN** the system returns 401

#### Scenario: SC011 - Revoked API key
- **WHEN** a request is made to any public endpoint with an `X-Api-Key` header containing a revoked key
- **THEN** the system returns 401

#### Scenario: SC012 - API key from a different team
- **WHEN** a request is made to `/api/events/{teamSlug}/{eventSlug}/...` with an API key that belongs to a team other than `{teamSlug}`
- **THEN** the system returns 403

#### Scenario: SC013 - Valid API key for correct team
- **WHEN** a request is made to `/api/events/{teamSlug}/{eventSlug}/...` with a valid, active API key belonging to `{teamSlug}`
- **THEN** the system proceeds to process the request normally

---

### Requirement: Public API routes are prefixed with `/api`
All public endpoints SHALL be accessible at paths beginning with `/api/`. The previous root-level paths (`/events/...`) SHALL no longer exist.

#### Scenario: SC014 - Public endpoint at /api prefix
- **WHEN** a valid request is sent to `/api/events/{teamSlug}/{eventSlug}/...` with a valid API key
- **THEN** the system processes the request and returns the appropriate response

#### Scenario: SC015 - Old root paths no longer exist
- **WHEN** a request is sent to `/events/{teamSlug}/{eventSlug}/...` (without `/api` prefix)
- **THEN** the system returns 404
