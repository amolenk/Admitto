# custom-bulk-templates Specification

## Purpose

TBD

## Requirements

### Requirement: Custom bulk email templates are first-class entities with CRUD support

The Email module SHALL persist `CustomBulkTemplate` records in the `email` schema. Each record SHALL carry a unique `Id`, `TeamId`, `TicketedEventId` (nullable — null for team-scoped templates), a user-supplied `Name`, `Subject`, `TextBody`, `HtmlBody`, `CreatedAt`, `UpdatedAt`, and a `Version` token for optimistic concurrency.

Admin endpoints SHALL be exposed under `/admin/teams/{teamSlug}/events/{eventSlug}/custom-bulk-templates` (event-scoped) and `/admin/teams/{teamSlug}/custom-bulk-templates` (team-scoped):

- `GET /` — list all custom bulk templates for the scope, ordered by name.
- `POST /` — create a new template. Request body: `{ name, subject, textBody, htmlBody }`. Returns `201 Created` with the new template id.
- `GET /{id}` — fetch a single template by id.
- `PUT /{id}` — update a template (full replace). Request body: `{ name, subject, textBody, htmlBody, version }`. Returns `200 OK`.
- `DELETE /{id}` — delete a template. Returns `204 No Content`.

All endpoints SHALL require team-membership authorisation on the owning team.

#### Scenario: Create a custom template

- **WHEN** an organizer posts `{ name: "Alumni invite", subject: "Join us in 2027!", textBody: "...", htmlBody: "..." }` to the event-scoped create endpoint
- **THEN** a `CustomBulkTemplate` row is persisted and the response is `201 Created` with the new id

#### Scenario: List returns all templates for the scope ordered by name

- **WHEN** an organizer calls `GET /admin/teams/acme/events/devconf-2026/custom-bulk-templates` and three templates exist
- **THEN** the response is a JSON array of three items ordered alphabetically by name

#### Scenario: Update template with correct version succeeds

- **WHEN** an organizer sends `PUT` with the current `Version` and updated subject
- **THEN** the template is updated and the response contains the new `Version`

#### Scenario: Update template with stale version is rejected

- **WHEN** an organizer sends `PUT` with an outdated `Version`
- **THEN** the response is a `409 Conflict` concurrency error

#### Scenario: Delete removes the template

- **WHEN** an organizer sends `DELETE /{id}`
- **THEN** the template row is removed and the response is `204 No Content`

#### Scenario: Non-team-member is denied

- **WHEN** a user who is not a member of the owning team calls any custom-bulk-templates endpoint
- **THEN** the response is `403 Forbidden`

---

### Requirement: Custom bulk template names are unique within their scope

Within a given event scope, no two `CustomBulkTemplate` records SHALL share the same `Name` (case-insensitive). The same uniqueness constraint applies within a team scope. The constraint SHALL be enforced at the domain level and surfaced as a validation error.

#### Scenario: Duplicate name within event scope is rejected

- **WHEN** an organizer creates a second template named "Alumni invite" for the same event
- **THEN** the request is rejected with a validation error indicating the name is already in use

#### Scenario: Same name in different events is allowed

- **WHEN** templates named "Alumni invite" exist for both "devconf-2026" and "devconf-2027"
- **THEN** both records persist successfully without conflict
