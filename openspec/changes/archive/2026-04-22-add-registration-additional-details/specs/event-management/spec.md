## ADDED Requirements

### Requirement: TicketedEvent owns the additional-detail schema
The `TicketedEvent` aggregate SHALL own an ordered `AdditionalDetailSchema` listing the additional information fields collected from attendees during registration. Each `AdditionalDetailField` SHALL carry a stable `Key` (kebab-case, immutable once persisted), a human-readable `Name` (editable), and a `MaxLength` integer in `[1, 4000]`. The schema SHALL allow at most 25 fields per event. Field `Key` and field `Name` SHALL each be unique within the schema (case-insensitive for `Name`).

The aggregate SHALL allow organizers (Owner or Organizer role) to atomically replace the entire schema via a single `UpdateAdditionalDetailSchema` admin command. Schema mutations SHALL be rejected when the event's status is Cancelled or Archived. Schema updates SHALL participate in optimistic concurrency via `TicketedEvent.Version`.

Removing a field from the schema SHALL leave any existing values for that field untouched on already-persisted registrations (see registration-additional-details for storage and presentation behaviour).

The detailed validation rules for additional detail values at registration time, the storage shape on registrations, and the public/admin surfaces are defined in the `registration-additional-details` capability.

#### Scenario: Configure an initial additional-detail schema
- **WHEN** an organizer of active event "DevConf" updates the schema to `[{ key: "dietary", name: "Dietary requirements", maxLength: 200 }, { key: "tshirt", name: "T-shirt size", maxLength: 5 }]`
- **THEN** `TicketedEvent` persists the schema in the supplied order and `Version` is incremented

#### Scenario: Reorder fields
- **WHEN** an organizer submits the same fields in reversed order
- **THEN** the schema is persisted in the new order

#### Scenario: Rename a field while keeping its key
- **WHEN** an organizer changes `name` of the field with `key: "dietary"` to "Dietary needs"
- **THEN** the field's `Key` remains "dietary" and existing registration values for that key remain accessible

#### Scenario: Rejected — duplicate key
- **WHEN** an organizer submits a schema containing two fields with `key: "dietary"`
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — duplicate name (case-insensitive)
- **WHEN** an organizer submits a schema containing fields named "Dietary" and "dietary"
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — invalid key format
- **WHEN** an organizer submits a field with `key: "Dietary Needs"`
- **THEN** the request is rejected with a validation error indicating the key must match `^[a-z0-9][a-z0-9-]{0,49}$`

#### Scenario: Rejected — too many fields
- **WHEN** an organizer submits a schema with 26 fields
- **THEN** the request is rejected with a validation error

#### Scenario: Rejected — event is Cancelled
- **WHEN** event "DevConf" has status Cancelled and an organizer attempts to update the additional-detail schema
- **THEN** the `TicketedEvent` rejects the mutation with reason "event not active"

#### Scenario: Rejected — concurrent update conflict
- **WHEN** an organizer submits a schema update with a stale `Version`
- **THEN** the request is rejected with a concurrency conflict
