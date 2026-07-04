## MODIFIED Requirements

### Requirement: Public API requires a valid team API key

All Partner API endpoints under `/api/` SHALL require a valid `X-Api-Key` header. The key SHALL be matched against active API keys and SHALL authenticate as the team that owns the key. Partner API endpoint handlers SHALL derive `TeamId` from the authenticated API-key principal; Partner API routes SHALL NOT include team ID or team slug.

Requests without a key, with an invalid key, or with a revoked key SHALL be rejected with HTTP 401. Requests with a valid API key for a team whose event/resource does not match the requested event SHALL be processed using the key owner's team scope and rejected by the normal resource lookup rules, typically HTTP 404.

#### Scenario: SC009 - No API key provided

- **WHEN** a request is made to any Partner API endpoint without an `X-Api-Key` header
- **THEN** the system returns 401

#### Scenario: SC010 - Invalid or unknown API key

- **WHEN** a request is made to any Partner API endpoint with an `X-Api-Key` header containing an unrecognized value
- **THEN** the system returns 401

#### Scenario: SC011 - Revoked API key

- **WHEN** a request is made to any Partner API endpoint with an `X-Api-Key` header containing a revoked key
- **THEN** the system returns 401

#### Scenario: SC012 - API key from a different team

- **WHEN** a request is made to `/api/events/{eventId}/...` with a valid API key that belongs to a team other than the event's team
- **THEN** the endpoint uses the API key owner's team scope and returns the same response as an event that cannot be found for that team

#### Scenario: SC013 - Valid API key for correct team

- **WHEN** a request is made to `/api/events/{eventId}/...` with a valid, active API key belonging to the event's team
- **THEN** the system proceeds to process the request normally

---

### Requirement: Public API routes are prefixed with `/api`

All Partner API endpoints SHALL be accessible at paths beginning with `/api/`. Partner API event-scoped routes SHALL use `/api/events/{eventId}/...` and SHALL NOT include `/teams/{teamId}` or a team slug. The previous root-level paths (`/events/...`) and previous team-scoped API paths (`/api/teams/{teamId}/events/{eventId}/...`) SHALL no longer exist.

#### Scenario: SC014 - Partner endpoint at /api prefix

- **WHEN** a valid request is sent to `/api/events/{eventId}/...` with a valid API key
- **THEN** the system processes the request and returns the appropriate response

#### Scenario: SC015 - Old root paths no longer exist

- **WHEN** a request is sent to `/events/{teamSlug}/{eventSlug}/...` without `/api` prefix
- **THEN** the system returns 404

#### Scenario: Team-scoped Partner API paths no longer exist

- **WHEN** a request is sent to `/api/teams/{teamId}/events/{eventId}/...`
- **THEN** the system returns 404
