## ADDED Requirements

### Requirement: Admin UI surfaces an "Add registration" affordance on the event registrations area
The Admin UI SHALL provide an "Add registration" affordance on the event's registrations area, reachable from the existing event navigation for a team's event. Activating it SHALL open a form (dialog or page) that lets a team admin submit a single new registration for that event.

The form SHALL collect:
- Attendee email (required, validated client-side as a well-formed email)
- One or more ticket-type selections, populated from the event's current ticket catalog (required, at least one selection)
- Additional details, rendered from the event's `AdditionalDetailSchema` using the same field types and length limits the public registration form already uses (optional)

Submission SHALL `POST` to the admin-add registration endpoint via the Admin UI's `apiClient` (not the generated OpenAPI SDK), consistent with other Admin UI features.

On success, the UI SHALL close the form, refresh the registrations list so the new registration appears, and surface a brief confirmation. On a 4xx response, the UI SHALL keep the form open and render the server's validation errors inline against the relevant fields, including the duplicate-email and "event not active" cases.

The visual design SHALL match existing admin features in look-and-feel (form controls, spacing, validation messaging) per the repo's design conventions.

#### Scenario: Successfully add a registration via the Admin UI
- **WHEN** an admin on team "acme" opens the registrations area for event "devconf", clicks "Add registration", fills in `email = "speaker@example.com"`, selects ticket type "Speaker Pass", and submits
- **THEN** the UI calls the admin-add endpoint, the form closes on success, and the registrations list shows "speaker@example.com" with ticket "Speaker Pass"

#### Scenario: Display client-side validation error on add
- **WHEN** an admin submits the add-registration form with an empty email
- **THEN** the form displays an inline validation error on the email field and does not call the backend

#### Scenario: Display server validation error — duplicate email
- **WHEN** an admin submits the add-registration form for an email that is already registered for the event and the backend responds with the "already registered" error
- **THEN** the form remains open and the duplicate-registration error is rendered inline against the email field

#### Scenario: Display server validation error — event not active
- **WHEN** an admin submits the add-registration form and the backend responds with the "event not active" error
- **THEN** the form remains open and the error is surfaced to the user

#### Scenario: Ticket selection sourced from the event's ticket catalog
- **WHEN** an admin opens the add-registration form for event "devconf"
- **THEN** the ticket-type selector lists the active ticket types currently configured on that event (cancelled ticket types are not selectable)

#### Scenario: Additional details rendered from the event schema
- **WHEN** an admin opens the add-registration form for an event whose `AdditionalDetailSchema` declares `dietary` (maxLength 200) and `tshirt` (maxLength 5)
- **THEN** the form renders inputs for `dietary` and `tshirt` with the appropriate length limits, and submitting valid values stores them on the new registration
