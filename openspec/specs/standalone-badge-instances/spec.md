# Standalone Badge Instances Specification

## Purpose

For *standalone* badge types (those not linked to a ticket type), organizers manage individual badge instances directly. A typical use case is a "Guest" badge type where a speaker informs the organizer they are bringing a partner — the organizer adds a badge instance for that guest. This capability covers creating, updating, deleting, and listing badge instances for a standalone badge type.

## Requirements

### Requirement: Organizer can add a badge instance to a standalone badge type
The system SHALL allow organizers (Owner or Organizer role) to add a badge instance to a standalone badge type by providing a `DisplayName` (required, max 200 characters) and an optional `Notes` (max 500 characters). The server SHALL generate a `BadgeInstanceId` (GUID) on creation. The command SHALL be rejected if the referenced badge type does not exist in the `BadgeEvent`'s badge types list, is of type TicketBased, or if the `BadgeEvent.Status` is not Active. Active-event and badge-type-kind validation SHALL be performed by loading the `BadgeEvent` aggregate.

#### Scenario: Add a badge instance to a standalone badge type
- **WHEN** an organizer adds a badge instance with `displayName = "Jane Doe"` and `notes = "Speaker's partner"` to standalone badge type "Guest Badge" on event "conf-2026" whose `BadgeEvent.Status` is Active
- **THEN** a badge instance is created with the provided display name, notes, and a server-assigned `id`

#### Scenario: Reject adding an instance to a ticket-based badge type
- **WHEN** an organizer attempts to add a badge instance to a ticket-based badge type "General Admission Badge"
- **THEN** the request is rejected with reason "badge type is not standalone"

#### Scenario: Reject adding an instance when display name is empty
- **WHEN** an organizer submits an add-instance request with an empty `displayName`
- **THEN** the request is rejected with a validation error

#### Scenario: Reject adding an instance when event is not Active
- **WHEN** event "conf-2026" has `BadgeEvent.Status` Archived and an organizer attempts to add a badge instance
- **THEN** the request is rejected with reason "event not active"

---

### Requirement: Organizer can update a badge instance
The system SHALL allow organizers to update the `DisplayName` and `Notes` of an existing badge instance identified by its `BadgeInstanceId`. The command SHALL be rejected if the `BadgeEvent.Status` is not Active. The request SHALL supply `expectedVersion` matching the current `BadgeInstance.Version`; a mismatch SHALL be rejected with a concurrency conflict error. Active-event validation SHALL be performed by loading the `BadgeEvent` aggregate.

#### Scenario: Update a badge instance's display name and notes
- **WHEN** an organizer updates badge instance {bi-id} on standalone badge type "Guest Badge" (Active event) with `displayName = "Jane Smith"` and `notes = "Updated notes"` and the correct `expectedVersion`
- **THEN** the badge instance's display name is updated to "Jane Smith" and notes to "Updated notes"

#### Scenario: Reject update when event is not Active
- **WHEN** event "conf-2026" has `BadgeEvent.Status` Archived and an organizer attempts to update a badge instance
- **THEN** the request is rejected with reason "event not active"

#### Scenario: Reject update with stale instance version
- **WHEN** an organizer submits an update with an `expectedVersion` that does not match the current `BadgeInstance.Version`
- **THEN** the request is rejected with a concurrency conflict error

---

### Requirement: Organizer can delete a badge instance
The system SHALL allow organizers to delete a badge instance identified by its `BadgeInstanceId`. The command SHALL be rejected if the `BadgeEvent.Status` is not Active. Active-event validation SHALL be performed by loading the `BadgeEvent` aggregate.

#### Scenario: Delete a badge instance
- **WHEN** an organizer deletes badge instance {bi-id} from standalone badge type "Guest Badge" on an Active event
- **THEN** the badge instance no longer exists

#### Scenario: Reject delete when event is not Active
- **WHEN** event "conf-2026" has `BadgeEvent.Status` Archived and an organizer attempts to delete a badge instance
- **THEN** the request is rejected with reason "event not active"

---

### Requirement: Team members can list badge instances for a standalone badge type
The system SHALL allow team members with Crew role or above to list all badge instances for a standalone badge type. The badge-type-kind check SHALL be performed by loading the `BadgeEvent` aggregate. Each instance SHALL include its `id`, `displayName`, `notes`, and `version`.

#### Scenario: List badge instances for a standalone badge type
- **WHEN** a Crew member lists badge instances for standalone badge type "Guest Badge" on event "conf-2026" which has instances "Jane Doe" and "John Smith"
- **THEN** both instances are returned with their id, display name, notes, and version

#### Scenario: List badge instances for a badge type with no instances
- **WHEN** a Crew member lists badge instances for standalone badge type "Guest Badge" which has no instances yet
- **THEN** an empty list is returned

#### Scenario: Reject listing instances for a ticket-based badge type
- **WHEN** a Crew member attempts to list badge instances for a ticket-based badge type
- **THEN** the request is rejected with reason "badge type is not standalone"
