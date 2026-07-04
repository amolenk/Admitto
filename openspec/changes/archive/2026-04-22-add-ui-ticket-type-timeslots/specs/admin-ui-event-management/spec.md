## ADDED Requirements

### Requirement: Add ticket type form supports time slots
The Admin UI **Add ticket type** dialog SHALL include a "Time slots" input that lets organizers attach zero or more time-slot slugs to a new ticket type. Each entered token SHALL be validated against the slug format `^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$` before it is accepted as a chip. The form SHALL submit the resulting array as `timeSlots` on the existing `POST /admin/teams/{teamSlug}/events/{eventSlug}/ticket-types` request, sending an empty array (not `null`) when no slots are entered.

#### Scenario: Add a ticket type with two time slots
- **WHEN** an organizer enters slug "vip", name "VIP Pass", and adds the time slots "morning" and "afternoon", then submits
- **THEN** the API receives a request with `timeSlots: ["morning", "afternoon"]` and the new ticket type appears in the list with both time slots

#### Scenario: Add a ticket type with no time slots
- **WHEN** an organizer adds a ticket type without entering any time slot
- **THEN** the API receives a request with `timeSlots: []` and the new ticket type is created without time slots

#### Scenario: Reject invalid time-slot token
- **WHEN** an organizer types "Morning Session!" in the time-slot input and confirms
- **THEN** the token is rejected inline (not added as a chip) with a message indicating the allowed slug format

---

### Requirement: Add ticket type form suggests time slots already used in the event
The "Time slots" input in the Admin UI **Add ticket type** dialog SHALL surface, as selectable suggestions, the deduplicated set of time-slot slugs currently used by other ticket types of the same event (sourced from the loaded `GET …/ticket-types` response). Selecting a suggestion SHALL add it as a chip exactly as if the organizer had typed it. Free-form entry SHALL remain available regardless of suggestions.

#### Scenario: Suggestions are drawn from existing ticket types
- **WHEN** an event has ticket types whose time slots are `["morning"]` and `["morning", "afternoon"]`, and an organizer opens the Add ticket type dialog
- **THEN** the time-slot input offers "morning" and "afternoon" as suggestions

#### Scenario: No suggestions when event has no time slots
- **WHEN** an organizer opens the Add ticket type dialog for an event whose ticket types have no time slots
- **THEN** the time-slot input shows no suggestions but still accepts free-form entry

---

### Requirement: Ticket type listing displays time slots
The Admin UI ticket types page SHALL render each ticket type's time slots as compact badges on the ticket type card. Cards for ticket types without time slots SHALL omit the badge row entirely.

#### Scenario: Card shows time slots
- **WHEN** the ticket types page renders a ticket type whose time slots are `["morning", "afternoon"]`
- **THEN** the card displays both slugs as badges in the card header area

#### Scenario: Card omits the row when no time slots
- **WHEN** the ticket types page renders a ticket type with an empty time slots list
- **THEN** the card does not display a time-slot badge row

---

### Requirement: Edit ticket type dialog shows time slots as read-only
The Admin UI **Edit ticket type** dialog SHALL display the ticket type's existing time slots as disabled chips together with a helper text indicating that time slots cannot be changed after creation. The edit submission SHALL NOT include a `timeSlots` field.

#### Scenario: Time slots are visible but not editable
- **WHEN** an organizer opens the Edit ticket type dialog for a ticket type with time slots `["morning"]`
- **THEN** the dialog shows a disabled "morning" chip and helper text explaining time slots are immutable, and submitting the form sends only the name and capacity fields

#### Scenario: Edit dialog hides the section when no time slots
- **WHEN** an organizer opens the Edit ticket type dialog for a ticket type with no time slots
- **THEN** the time-slot section is omitted
