## ADDED Requirements

### Requirement: Admin can directly add a registration to a ticketed event
The system SHALL provide an admin-only command and HTTP endpoint that creates a `Registration` for a given attendee email and ticket-type selection on a specific ticketed event, without requiring a coupon and without going through the public self-service flow.

The admin path SHALL bypass the registration window, the per-ticket-type capacity limit, the requirement that selected ticket types have an explicit capacity configured, and any configured email-domain restriction.

The admin path SHALL still enforce all of the following:
- The event MUST be loadable; otherwise the registration is rejected with reason "event not found".
- `TicketedEvent.Status` MUST be Active; Cancelled or Archived events SHALL reject the registration with reason "event not active".
- The event MUST have a `TicketCatalog` with at least one ticket type configured; otherwise the registration is rejected with reason "no ticket types configured".
- Ticket-type selection rules from `attendee-registration` (no duplicates, no unknown types, no cancelled types, no overlapping time slots).
- The duplicate-email guard (one registration per email per event), enforced by the existing unique constraint at persist time.
- Validation of `additionalDetails` against the event's current `AdditionalDetailSchema` (unknown keys rejected, value-length limits enforced).

The admin path SHALL increment the used capacity for each selected ticket type (using the same unenforced-claim mechanism that coupons use), so that attendance counts remain accurate even though the limit itself is not enforced. The atomic claim against `TicketCatalog` SHALL still trip the `TicketCatalog.EventStatus` safety net so a concurrent cancel/archive cannot leak a registration through.

The admin path SHALL produce the same post-creation effects as the other registration paths (e.g. domain/module events that drive the standard confirmation email flow) — there is no "silent add" mode in this change.

#### Scenario: Successful admin-add registration
- **WHEN** an admin adds a registration as "speaker@example.com" for "Speaker Pass" on event "DevConf" with `TicketedEvent.Status` Active and "Speaker Pass" capacity 5/5 used
- **THEN** a registration is created for "speaker@example.com" with ticket "Speaker Pass" and "Speaker Pass" capacity used increases to 6

#### Scenario: Admin-add bypasses registration window — before opens
- **WHEN** an admin adds a registration for an event whose registration window opens tomorrow
- **THEN** the registration is created

#### Scenario: Admin-add bypasses registration window — already closed
- **WHEN** an admin adds a registration for an event whose registration window closed yesterday
- **THEN** the registration is created

#### Scenario: Admin-add bypasses registration window — never configured
- **WHEN** an admin adds a registration for an event with no registration window configured
- **THEN** the registration is created

#### Scenario: Admin-add bypasses email-domain restriction
- **WHEN** an admin adds a registration as "external@gmail.com" for event "CorpConf" which is restricted to "@acme.com"
- **THEN** the registration is created for "external@gmail.com"

#### Scenario: Admin-add bypasses capacity limit
- **WHEN** an admin adds a registration for "Workshop" where capacity is 20/20 used
- **THEN** the registration is created and capacity used increases to 21

#### Scenario: Admin-add bypasses missing capacity configuration
- **WHEN** an admin adds a registration for "Speaker Pass" which has no capacity configured
- **THEN** the registration is created

#### Scenario: Admin-add rejected — event not active (Cancelled)
- **WHEN** an admin attempts to add a registration to event "OldConf" whose `TicketedEvent.Status` is Cancelled
- **THEN** the registration is rejected with reason "event not active"

#### Scenario: Admin-add rejected — event not active (Archived)
- **WHEN** an admin attempts to add a registration to event "OldConf" whose `TicketedEvent.Status` is Archived
- **THEN** the registration is rejected with reason "event not active"

#### Scenario: Admin-add rejected — event not found
- **WHEN** an admin attempts to add a registration for an event id that does not exist
- **THEN** the registration is rejected with reason "event not found"

#### Scenario: Admin-add rejected — no ticket types configured
- **WHEN** an admin attempts to add a registration for an event that has no `TicketCatalog` ticket types
- **THEN** the registration is rejected with reason "no ticket types configured"

#### Scenario: Admin-add rejected — duplicate email
- **WHEN** "alice@example.com" is already registered for event "DevConf" and an admin attempts to add another registration for "alice@example.com" on the same event
- **THEN** the registration is rejected with reason "already registered"

#### Scenario: Admin-add rejected — duplicate ticket types in selection
- **WHEN** an admin attempts to add a registration selecting "General Admission" twice
- **THEN** the registration is rejected with reason "duplicate ticket types"

#### Scenario: Admin-add rejected — unknown ticket type
- **WHEN** an admin attempts to add a registration selecting ticket type "Premium VIP" which does not exist on the event
- **THEN** the registration is rejected with reason "unknown ticket type"

#### Scenario: Admin-add rejected — cancelled ticket type
- **WHEN** an admin attempts to add a registration selecting "Workshop A" which has been cancelled
- **THEN** the registration is rejected with reason "ticket type cancelled"

#### Scenario: Admin-add rejected — overlapping time slots
- **WHEN** an admin attempts to add a registration selecting "Workshop A" and "Workshop B" which share a time slot
- **THEN** the registration is rejected with reason "overlapping time slots"

#### Scenario: Admin-add rejected — additional detail key not in schema
- **WHEN** an admin attempts to add a registration with `{ "shoesize": "44" }` and the event's schema has no `shoesize` field
- **THEN** the registration is rejected with reason "additional detail key not in schema"

#### Scenario: Admin-add rejected — additional detail value too long
- **WHEN** an admin attempts to add a registration with `{ "tshirt": "XXXXL-extra-long" }` and the `tshirt` field has `maxLength: 5`
- **THEN** the registration is rejected with reason "additional detail value too long"

#### Scenario: Concurrent cancel detected at claim time
- **WHEN** an admin adds a registration and `TicketedEvent.Status` is Active at policy-check time but `TicketCatalog.EventStatus` has been transitioned to Cancelled by an in-flight cancel before the claim commits
- **THEN** the registration fails with reason "event not active" and no capacity is consumed

---

### Requirement: Admin-add registration is exposed via an admin HTTP endpoint
The system SHALL expose a `POST /admin/teams/{teamSlug}/events/{eventSlug}/registrations` admin HTTP endpoint that invokes the admin-add registration command. The endpoint SHALL be protected by the same team-admin authorisation policy used by the rest of the module's admin endpoints. Request validation (well-formed email, at least one non-empty ticket-type slug) SHALL run in the endpoint filter via FluentValidation before the handler executes. On success, the endpoint SHALL return `201 Created` with the new registration's identifier in the response body.

#### Scenario: Endpoint requires admin authentication
- **WHEN** an unauthenticated client calls `POST /admin/teams/acme/events/devconf/registrations`
- **THEN** the endpoint responds with `401 Unauthorized`

#### Scenario: Endpoint rejects a request with an invalid email
- **WHEN** an admin calls the endpoint with `email = "not-an-email"`
- **THEN** the endpoint responds with `400 Bad Request` and a validation error on the `email` field, and no registration is created

#### Scenario: Endpoint rejects a request with no ticket types
- **WHEN** an admin calls the endpoint with `ticketTypeSlugs = []`
- **THEN** the endpoint responds with `400 Bad Request` and a validation error on the `ticketTypeSlugs` field, and no registration is created

#### Scenario: Endpoint returns 201 with the new registration id
- **WHEN** an admin calls the endpoint with a valid request for an Active event
- **THEN** the endpoint responds with `201 Created` and a body containing the new registration id

---

### Requirement: Admin-add registration is exposed via the CLI
The Admitto CLI SHALL expose a command that adds a registration via the new admin HTTP endpoint, calling it through the regenerated NSwag `ApiClient` with no business logic beyond input mapping, slug resolution, and output formatting. The same change that introduces the endpoint SHALL regenerate `ApiClient.g.cs` via `generate-api-client.sh` and SHALL add the matching command, in line with the `cli-admin-parity` capability.

#### Scenario: Operator adds a registration from the CLI
- **WHEN** an operator runs the CLI command to add a registration for team "acme", event "devconf", email "speaker@example.com", and ticket-type slug "speaker-pass"
- **THEN** the CLI SHALL call `POST /admin/teams/acme/events/devconf/registrations` via `ApiClient` with the supplied email and ticket-type selection
- **AND** SHALL print the new registration id on success or the server-reported error on failure
