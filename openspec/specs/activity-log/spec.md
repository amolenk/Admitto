# Activity Log Specification

## Purpose

The ActivityLog is a read-side projection in the Registrations module that records immutable lifecycle milestones for each registration. It is driven by domain events and provides an accurate, append-only history of what happened to a registration over time — supporting future scenarios where the same registration can be cancelled and then re-activated (e.g. re-registration).

## Requirements

### Requirement: ActivityLog records lifecycle milestones for each registration

The system SHALL maintain an `activity_log` table in the Registrations module. Each row represents one milestone in a registration's lifecycle and SHALL include: `id`, `registration_id`, `activity_type` (one of `Registered`, `Reconfirmed`, `Cancelled`), `occurred_at` (the timestamp from the originating event), and optional `metadata` (e.g. cancellation reason as a string).

Entries are append-only. No entry is ever updated or deleted.

Three domain event handlers SHALL project into this table:

- `AttendeeRegisteredDomainEvent` → entry with `activity_type=Registered`, `occurred_at` = event creation timestamp
- `RegistrationReconfirmedDomainEvent` → entry with `activity_type=Reconfirmed`, `occurred_at` = reconfirmed timestamp
- `RegistrationCancelledDomainEvent` → entry with `activity_type=Cancelled`, `occurred_at` = event timestamp, `metadata` = cancellation reason string

Each handler runs in the same database transaction as the aggregate change, so no separate idempotency guard is required.

#### Scenario: SC001 Registration event projects a Registered entry

- **WHEN** a new registration is created, raising `AttendeeRegisteredDomainEvent`
- **THEN** an `activity_log` row exists with `activity_type=Registered`, `registration_id` matching the new registration, and `occurred_at` set to the registration creation time

#### Scenario: SC002 Reconfirmation event projects a Reconfirmed entry

- **WHEN** an attendee reconfirms, raising `RegistrationReconfirmedDomainEvent`
- **THEN** an `activity_log` row exists with `activity_type=Reconfirmed` and `occurred_at` set to the reconfirmed-at timestamp

#### Scenario: SC003 Cancellation event projects a Cancelled entry with reason

- **WHEN** a registration is cancelled with reason `AttendeeRequest`, raising `RegistrationCancelledDomainEvent`
- **THEN** an `activity_log` row exists with `activity_type=Cancelled`, `occurred_at` set to the cancellation timestamp, and `metadata` containing the reason `AttendeeRequest`

#### Scenario: SC004 Duplicate scenario not applicable

- Domain event handlers run in the same database transaction as the aggregate change, so duplicate entries cannot occur under normal operation. No idempotency guard is needed.

#### Scenario: SC005 Multiple milestones accumulate for the same registration

- **GIVEN** a registration that was registered, then reconfirmed, then cancelled
- **THEN** three `activity_log` rows exist for that registration, one per milestone, each with the correct type and timestamp

### Requirement: ActivityLog entries are queried as part of registration detail

The system SHALL include `ActivityLog` entries for a given `registrationId` when responding to the `GetRegistrationDetails` query. Entries SHALL be returned ordered by `occurred_at` ascending.

#### Scenario: SC006 Registration detail response includes activity entries

- **GIVEN** a registration with Registered and Reconfirmed entries in the activity log
- **WHEN** an admin queries the registration detail
- **THEN** the response includes both activity entries ordered oldest-first
