## Purpose

Per-event, optional additional information collected from attendees at registration time. This capability defines the schema model that lives on the `TicketedEvent`, the storage of collected values on `Registration`, the validation rules applied at registration time, the public surfaces (event-info, public registration), the admin surfaces (attendee/registration views), and CLI parity. All values are strings; "required" semantics are delegated to the event's public website.

## ADDED Requirements

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

### Requirement: Public event-info exposes the current schema
The public event-info / availability response SHALL include an `additionalDetails` array describing the current schema in display order. Each entry SHALL include `key`, `name`, `maxLength`, and `order` (the array index).

#### Scenario: Schema is exposed to the event website
- **WHEN** an event website fetches event-info for "DevConf" whose schema is `[dietary (200), tshirt (5)]`
- **THEN** the response includes `additionalDetails: [{ key: "dietary", name: "Dietary requirements", maxLength: 200, order: 0 }, { key: "tshirt", name: "T-shirt size", maxLength: 5, order: 1 }]`

### Requirement: Admin views surface current and historical additional details
Admin attendee/registration detail responses SHALL return two payloads: `additionalDetails` (values whose keys are present in the event's current schema, returned in schema order with the field `Name`) and `historicalAdditionalDetails` (values whose keys are no longer in the schema, returned with the bare `key`). The Admin UI SHALL render both, marking historical entries as orphaned.

#### Scenario: Admin sees current and historical values
- **GIVEN** registration `r1` has `AdditionalDetails = { "dietary": "vegan", "shoesize": "44" }` and the schema currently declares only `dietary`
- **WHEN** an admin opens the registration detail view for `r1`
- **THEN** the response returns `additionalDetails: [{ key: "dietary", name: "Dietary requirements", value: "vegan" }]` and `historicalAdditionalDetails: [{ key: "shoesize", value: "44" }]`

### Requirement: Schema editor is the only path to mutate the schema
The system SHALL expose exactly one admin command (`UpdateAdditionalDetailSchema`) and corresponding HTTP endpoint to mutate the schema. Mutations SHALL replace the schema atomically with the supplied ordered list. There SHALL NOT be per-field add/remove/rename endpoints.

#### Scenario: Single atomic update
- **WHEN** an organizer submits an updated schema list for event "DevConf"
- **THEN** the system replaces the entire schema atomically and emits a single `AdditionalDetailSchemaUpdated` domain event

### Requirement: CLI parity for schema management
The CLI SHALL provide commands to view and replace the additional-detail schema for an event, mirroring the admin HTTP surface. Listing SHALL render the current schema in display order. Setting SHALL accept a JSON file describing the full ordered list and SHALL submit the event's current `Version` for optimistic concurrency.

Note: Delivery of CLI commands depends on the broader ApiClient regeneration follow-up tracked outside this change.

#### Scenario: List the current schema
- **WHEN** an admin runs `admitto event additional-details list --team acme --event devconf`
- **THEN** the CLI prints the schema fields in display order with their `key`, `name`, and `maxLength`

#### Scenario: Replace the schema from a JSON file
- **WHEN** an admin runs `admitto event additional-details set --team acme --event devconf --from-json schema.json`
- **THEN** the CLI submits the contents as the new schema with the current `Version` and reports success or a concurrency error
