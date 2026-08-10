## ADDED Requirements

### Requirement: Partner can retrieve reduced ticketed-event details by event slug
The system SHALL expose `GET /api/events/{eventSlug}` as a Partner API endpoint that requires a valid active `X-Api-Key`.

The endpoint SHALL derive `TeamId` from the authenticated API-key principal and SHALL resolve `{eventSlug}` to the internal ticketed event within that team scope. The endpoint SHALL NOT require an `Authorization` bearer token or email-verification token.

The response SHALL contain exactly the reduced event metadata needed by partner event websites:
- `name`: event display name
- `slug`: public event slug
- `startsAt`: event start timestamp
- `endsAt`: event end timestamp
- `timeZone`: event IANA timezone id
- `isRegistrationOpen`: whether registration is currently open
- `allowedEmailDomain`: the configured allowed email domain, or `null` when unrestricted
- `additionalDetailFields`: ordered list of additional-detail fields, each with `key`, `name`, and `maxLength`; empty when the event has no additional-detail schema

The response SHALL NOT include internal fields such as the internal event id, team id, version, lifecycle status, reconfirm policy, or waitlist policy.

#### Scenario: Existing event returns reduced details
- **WHEN** a partner website calls `GET /api/events/{eventSlug}` with a valid API key for the event's team
- **THEN** the system returns 200 with the event's name, slug, startsAt, endsAt, timeZone, isRegistrationOpen, allowedEmailDomain, and additionalDetailFields only

#### Scenario: Additional detail fields returned in schema order
- **WHEN** the event has an additional-detail schema with multiple fields
- **THEN** `additionalDetailFields` lists each field's key, name, and maxLength in the schema's display order

#### Scenario: Empty field list and null domain when unconfigured
- **WHEN** the event has no additional-detail schema and no allowed email domain
- **THEN** `additionalDetailFields` is an empty array and `allowedEmailDomain` is `null`

#### Scenario: Internal fields are not returned
- **WHEN** a partner website retrieves event details
- **THEN** the response does not include the internal event id, team id, version, lifecycle status, reconfirm policy, or waitlist policy

#### Scenario: Unknown event slug returns not found
- **WHEN** a partner website calls `GET /api/events/{eventSlug}` for a slug that does not exist for the API key's team
- **THEN** the system returns 404 Not Found

#### Scenario: Missing API key is rejected
- **WHEN** the request omits a valid `X-Api-Key`
- **THEN** the system returns 401 Unauthorized
