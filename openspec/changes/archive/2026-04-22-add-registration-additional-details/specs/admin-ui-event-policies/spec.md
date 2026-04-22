## ADDED Requirements

### Requirement: Admin can manage the additional-detail schema from the registration policy page
The Admin UI SHALL extend the Registration Policy page with an "Additional details" section that lets organizers add, rename, reorder, and remove additional detail fields. Each row SHALL display the field's `Name`, `Key`, and `MaxLength`. Adding a field SHALL auto-generate the `Key` from the `Name` (kebab-case) and SHALL allow the organizer to override it before the field is first persisted; once persisted the `Key` SHALL be read-only.

The form SHALL submit the entire ordered field list together with the event's current `TicketedEvent.Version` for optimistic concurrency. On success the UI SHALL show a confirmation message and refresh the displayed values.

Removing a field SHALL require an explicit confirmation that informs the organizer that historical values for that field will be preserved on existing registrations but will no longer be collected for new registrations.

When `TicketedEvent.Status` is Cancelled or Archived, the editor SHALL be read-only and SHALL display a banner indicating the event is not active.

#### Scenario: Add a new additional detail field
- **WHEN** an organizer of team "acme" opens the Registration Policy page for active event "DevConf", adds a field named "Dietary requirements" with maxLength 200, and submits
- **THEN** the schema is saved with a new field whose key is auto-generated as "dietary-requirements"

#### Scenario: Override the auto-generated key before persisting
- **WHEN** an organizer adds a new field named "Dietary requirements" and edits the auto-generated key to "dietary" before submitting
- **THEN** the schema is saved with the field's key as "dietary"

#### Scenario: Reorder fields
- **WHEN** an organizer drags the "T-shirt size" row above the "Dietary requirements" row and submits
- **THEN** the schema is persisted in the new order

#### Scenario: Rename a field without changing its key
- **WHEN** an organizer changes the name of the persisted field with key "dietary" to "Dietary needs" and submits
- **THEN** the schema is saved and the field's key remains "dietary"

#### Scenario: Remove a field requires confirmation
- **WHEN** an organizer clicks the remove button for the field with key "dietary"
- **THEN** the UI shows a confirmation dialog explaining that historical values will be preserved but no longer collected
- **AND** removal proceeds only after the organizer confirms

#### Scenario: Editor is read-only for cancelled events
- **WHEN** an organizer opens the Registration Policy page for event "DevConf" whose `TicketedEvent.Status` is Cancelled
- **THEN** the additional-details rows are read-only and a banner indicates the event is cancelled

#### Scenario: Concurrency conflict surfaces to the user
- **WHEN** an organizer submits the additional-details form but the backend rejects the write with a concurrency conflict
- **THEN** the UI shows an error prompting the user to reload the page
