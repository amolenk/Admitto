# Badge Type Management Specification

## Purpose

Organizers define badge types for a ticketed event. A badge type is either *ticket-based* (generates one badge per registration of a specified ticket type) or *standalone* (a manually managed list of badge instances with no ticket link — e.g. "Guest", "Speaker's Partner"). This capability covers the full lifecycle of badge types: creation, update, deletion, and listing.

## ADDED Requirements

### Requirement: Organizer can add a ticket-based badge type to an event
The system SHALL allow organizers (Owner or Organizer role) to add a ticket-based badge type to an event by providing a name and a non-empty list of `TicketTypeId`s, each referencing a ticket type that belongs to that event. The server SHALL generate a `BadgeTypeId` (GUID) on creation. Badge type names SHALL be unique within an event (case-insensitive). The list of `TicketTypeId`s SHALL contain at least one entry. The command SHALL be rejected when the event's `BadgesEvent.Status` is not Active.

#### Scenario: Add a ticket-based badge type linked to a single ticket type
- **WHEN** an organizer adds a badge type with name "General Admission Badge" and `ticketTypeIds = [<id of "General Admission">]` on event "conf-2026" whose `BadgesEvent.Status` is Active
- **THEN** a badge type is created with type `TicketBased`, the provided name and ticket type list, and a server-assigned `id`

#### Scenario: Add a ticket-based badge type linked to multiple ticket types
- **WHEN** an organizer adds a badge type with name "Conference Badge" and `ticketTypeIds = [<id of "Workshop">, <id of "Conference">]` on event "conf-2026" whose `BadgesEvent.Status` is Active
- **THEN** a badge type is created with type `TicketBased` linked to both ticket types

#### Scenario: Reject adding a ticket-based badge type with an empty ticket type list
- **WHEN** an organizer adds a ticket-based badge type with `ticketTypeIds = []`
- **THEN** the request is rejected with a validation error

#### Scenario: Reject adding a badge type with a duplicate name
- **WHEN** event "conf-2026" already has a badge type named "VIP Badge" and an organizer adds another with name "VIP Badge" (or "vip badge" — case-insensitive)
- **THEN** the request is rejected with reason "badge type name already exists"

#### Scenario: Reject adding a badge type when event is not Active
- **WHEN** event "conf-2026" has `BadgesEvent.Status` Archived and an organizer attempts to add a badge type
- **THEN** the request is rejected with reason "event not active"

---

### Requirement: Organizer can add a standalone badge type to an event
The system SHALL allow organizers to add a standalone badge type (no ticket link) by providing only a name. The server SHALL generate a `BadgeTypeId` on creation. The command SHALL be rejected when the event's `BadgesEvent.Status` is not Active.

#### Scenario: Add a standalone badge type to an active event
- **WHEN** an organizer adds a badge type with name "Guest Badge" and no `ticketTypeId` on event "conf-2026" whose `BadgesEvent.Status` is Active
- **THEN** a badge type is created with type `Standalone`, the provided name, and a server-assigned `id`

#### Scenario: Reject adding a standalone badge type when event is not Active
- **WHEN** event "conf-2026" has `BadgesEvent.Status` Archived and an organizer attempts to add a standalone badge type
- **THEN** the request is rejected with reason "event not active"

---

### Requirement: Organizer can update a badge type's name
The system SHALL allow organizers to rename a badge type identified by its `BadgeTypeId`. The updated name SHALL remain unique within the event (case-insensitive). The command SHALL be rejected when the event's `BadgesEvent.Status` is not Active. The type (TicketBased / Standalone) and the `TicketTypeId` link SHALL NOT be changeable after creation.

#### Scenario: Rename a badge type
- **WHEN** an organizer updates badge type with id {bt-id} on event "conf-2026" (Active) to name "Speaker Badge"
- **THEN** the badge type name is updated to "Speaker Badge"

#### Scenario: Reject rename to a name already taken
- **WHEN** event "conf-2026" already has a badge type named "VIP Badge" and an organizer renames badge type {bt-id} to "VIP Badge"
- **THEN** the request is rejected with reason "badge type name already exists"

#### Scenario: Reject rename when event is not Active
- **WHEN** event "conf-2026" has `BadgesEvent.Status` Archived and an organizer attempts to rename a badge type
- **THEN** the request is rejected with reason "event not active"

---

### Requirement: Organizer can delete a badge type
The system SHALL allow organizers to delete a badge type identified by its `BadgeTypeId`. Deleting a standalone badge type SHALL also delete all its `BadgeInstance` records. Deleting a ticket-based badge type SHALL succeed regardless of how many registrations reference its ticket type (badge types are configuration, not historical records). The command SHALL be rejected when the event's `BadgesEvent.Status` is not Active.

#### Scenario: Delete a standalone badge type and its instances
- **WHEN** event "conf-2026" has a standalone badge type "Guest Badge" with 3 instances and an organizer deletes it
- **THEN** the badge type and all 3 instances are deleted

#### Scenario: Delete a ticket-based badge type
- **WHEN** event "conf-2026" has a ticket-based badge type "General Admission Badge" and an organizer deletes it
- **THEN** the badge type is deleted; existing registrations are unaffected

#### Scenario: Reject delete when event is not Active
- **WHEN** event "conf-2026" has `BadgesEvent.Status` Archived and an organizer attempts to delete a badge type
- **THEN** the request is rejected with reason "event not active"

---

### Requirement: Team members can list badge types for an event
The system SHALL allow team members with Crew role or above to list all badge types for an event. Each badge type in the response SHALL include its `id`, `name`, `type` (TicketBased or Standalone), `ticketTypeIds` (empty list for standalone), and for standalone types the count of badge instances.

#### Scenario: List badge types for an event
- **WHEN** a Crew member lists badge types for event "conf-2026" which has "Conference Badge" (TicketBased, linked to 2 ticket types) and "Guest Badge" (Standalone, 2 instances)
- **THEN** both badge types are returned with their respective type, ticket type id list (2 entries for "Conference Badge", empty for "Guest Badge"), and instance count

#### Scenario: List badge types for an event with no badge types
- **WHEN** a Crew member lists badge types for event "conf-2026" which has no badge types defined
- **THEN** an empty list is returned

---

### Requirement: BadgesEvent entity tracks lifecycle for mutation gating
The Badges module SHALL maintain a `BadgesEvent` entity (columns: `EventId`, `Status`) per ticketed event. The entity SHALL be created with `Status = Active` in response to the `TicketedEventCreated` integration event from Registrations. The `Status` SHALL transition to `Archived` in response to both `TicketedEventCancelled` and `TicketedEventArchived` integration events. Badge-type mutation commands SHALL load this entity and reject when `Status` is not Active.

#### Scenario: BadgesEvent is created Active on event creation
- **WHEN** the Badges module processes the `TicketedEventCreated` integration event for event {event-id}
- **THEN** a `BadgesEvent` with `EventId = {event-id}` and `Status = Active` exists

#### Scenario: BadgesEvent transitions to Archived on event cancellation
- **WHEN** the Badges module processes the `TicketedEventCancelled` integration event for event {event-id}
- **THEN** the `BadgesEvent` for {event-id} has `Status = Archived`

#### Scenario: BadgesEvent transitions to Archived on event archival
- **WHEN** the Badges module processes the `TicketedEventArchived` integration event for event {event-id}
- **THEN** the `BadgesEvent` for {event-id} has `Status = Archived`
