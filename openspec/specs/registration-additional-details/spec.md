# Registration Additional Details Specification

## Purpose

Per-event, optional additional information collected from attendees at registration time. This capability defines the schema model that lives on the `TicketedEvent`, the storage of collected values on `Registration`, and the validation rules applied at registration time. All values are strings; "required" semantics are delegated to the event's public website.

The public event-info exposure of the schema, the admin read model for registration detail views, and CLI parity are tracked under a follow-up change and are intentionally not part of this spec yet.

## Requirements

### Requirement: Additional detail values are stored on the Registration aggregate
The `Registration` aggregate SHALL persist accepted additional detail values as an `AdditionalDetails` value object, modelled as an immutable `IReadOnlyDictionary<string, string>` keyed by the field's `Key`. The dictionary SHALL be persisted to a single `jsonb` column on the `registrations` table. Keys absent from the dictionary SHALL be treated as "not provided"; empty strings SHALL be preserved as empty strings.

#### Scenario: Values are persisted with the registration
- **WHEN** a registration is created with `AdditionalDetails = { "dietary": "vegan", "tshirt": "M" }`
- **THEN** the registration row's additional-details `jsonb` column contains exactly those two key/value pairs

#### Scenario: Empty-string values are preserved
- **WHEN** a registration is created with `AdditionalDetails = { "dietary": "" }`
- **THEN** the persisted `jsonb` column contains `dietary` mapped to the empty string

### Requirement: Removing a field preserves historical values
When the event's additional-detail schema is updated to remove a field, the system SHALL leave existing values for that field's key intact on previously persisted registrations. The system SHALL NOT modify historical registration rows. New registrations SHALL reject any attempt to submit a value for the removed key (per attendee-registration's "key not in schema" rule).

#### Scenario: Historical values survive a schema removal
- **GIVEN** registration `r1` has `AdditionalDetails = { "dietary": "vegan" }` and the event's schema currently declares the `dietary` field
- **WHEN** an organizer removes the `dietary` field from the schema
- **THEN** `r1`'s additional-details column still contains `{ "dietary": "vegan" }`

#### Scenario: New registrations cannot resurrect a removed key
- **GIVEN** the event's schema no longer declares the `dietary` field
- **WHEN** an attendee self-registers with `{ "dietary": "vegan" }`
- **THEN** the registration is rejected with reason "additional detail key not in schema"

### Requirement: Schema editor is the only path to mutate the schema
The system SHALL expose exactly one admin command (`UpdateAdditionalDetailSchema`) and corresponding HTTP endpoint to mutate the schema. Mutations SHALL replace the schema atomically with the supplied ordered list. There SHALL NOT be per-field add/remove/rename endpoints.

#### Scenario: Single atomic update
- **WHEN** an organizer submits an updated schema list for event "DevConf"
- **THEN** the system replaces the entire schema atomically and emits a single `AdditionalDetailSchemaUpdated` domain event
