## ADDED Requirements

### Requirement: TicketedEvent owns a per-event signing key generated at creation

The `TicketedEvent` aggregate SHALL carry a `SigningKey` value generated at creation time using a cryptographically-secure random source with at least 32 bytes (256 bits) of entropy. The key SHALL be unique per event and SHALL be assigned automatically by the aggregate's factory method — callers SHALL NOT supply the key.

The `SigningKey` SHALL NOT be exposed via:
- public read DTOs of any endpoint, admin or otherwise;
- integration events published from the Registrations module;
- the Organization module's view of ticketed events (slug/id resolution only);
- any structured log entry written by application code.

The `SigningKey` SHALL be persisted in the Registrations schema as a `NOT NULL` column on the `ticketed_events` table.

Existing `TicketedEvent` rows that predate this requirement SHALL be migrated by populating each row with a freshly-generated key in the same schema migration that introduces the column. The migration SHALL transition the column to `NOT NULL` only after every existing row has a value.

#### Scenario: Newly-created event has a signing key
- **WHEN** the Registrations module materialises a new `TicketedEvent` in response to a `TicketedEventCreationRequested` integration event
- **THEN** the resulting aggregate has a non-empty `SigningKey` whose decoded byte length is at least 32

#### Scenario: Each event gets its own key
- **WHEN** two `TicketedEvent`s are created in rapid succession on the same team
- **THEN** their `SigningKey` values differ

#### Scenario: Migration backfills existing events
- **WHEN** the schema migration that introduces `signing_key` runs against a database with pre-existing `ticketed_events` rows
- **THEN** every existing row receives a freshly-generated key before the column is altered to `NOT NULL`, and the migration is safe to retry

#### Scenario: Signing key is not exposed in event details
- **WHEN** an admin retrieves event details via the admin API
- **THEN** the response contains no field carrying the signing key or any value derived from it
