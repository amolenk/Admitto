## MODIFIED Requirements

### Requirement: Organizer can add ticket types to an event
The system SHALL allow organizers to add a ticket type to an active event with a
slug, name, time slots, and optional capacity. The `IsSelfService` and
`IsSelfServiceAvailable` flags are removed. Ticket type slugs SHALL be unique
within an event. The system SHALL reject adding ticket types to cancelled or
archived events. When a ticket type is successfully added, the system SHALL publish
a `TicketTypeAddedModuleEvent` containing the event id, ticket type slug, name,
time slots, and capacity so that the Registrations module can initialize its
capacity record.

#### Scenario: Add a ticket type to an active event
- **WHEN** an organizer adds a ticket type with slug "vip", name "VIP Pass", time slots ["morning", "afternoon"], and capacity 100 to event "conf-2026"
- **THEN** the event has a ticket type "vip" with the provided details and a `TicketTypeAddedModuleEvent` is published

#### Scenario: Add a ticket type with no capacity
- **WHEN** an organizer adds a ticket type with slug "speaker", name "Speaker Pass", and no capacity to event "conf-2026"
- **THEN** the event has a ticket type "speaker" with no capacity and a `TicketTypeAddedModuleEvent` is published with null capacity

#### Scenario: Reject duplicate ticket type slug
- **WHEN** event "conf-2026" already has a ticket type with slug "vip" and an organizer adds another with slug "vip"
- **THEN** the request is rejected with a duplicate ticket type slug error

#### Scenario: Reject adding ticket type to cancelled event
- **WHEN** an organizer attempts to add a ticket type to a cancelled event
- **THEN** the request is rejected because the event is cancelled

---

### Requirement: Organizer can update ticket types
The system SHALL allow organizers to update a ticket type's name and capacity.
The `IsSelfServiceAvailable` flag is removed. Ticket type slugs SHALL be
immutable after creation. When capacity is changed, the system SHALL publish a
`TicketTypeCapacityChangedModuleEvent` so that the Registrations module can update
its capacity ceiling.

#### Scenario: Update a ticket type's capacity
- **WHEN** an organizer updates ticket type "vip" to capacity 200
- **THEN** the ticket type capacity is changed to 200 and a `TicketTypeCapacityChangedModuleEvent` is published

#### Scenario: Update a ticket type's name (no capacity change)
- **WHEN** an organizer updates ticket type "vip" name to "VIP Access" without changing capacity
- **THEN** the ticket type name is updated and no `TicketTypeCapacityChangedModuleEvent` is published

#### Scenario: Reject changing a ticket type's slug
- **WHEN** an organizer attempts to change the slug of ticket type "vip" to "premium"
- **THEN** the request is rejected because ticket type slugs are immutable

---

## ADDED Requirements

### Requirement: Capacity tracking is synchronized with ticket type changes
The Registrations module SHALL maintain a capacity record per ticket type, keyed by
slug, which is initialized and updated in response to module events published by the
Organization module. If no capacity record exists for a slug when a
`TicketTypeAddedModuleEvent` arrives, the system SHALL create one. If a record
already exists, it SHALL be updated.

#### Scenario: Capacity record created on TicketTypeAddedModuleEvent
- **WHEN** a `TicketTypeAddedModuleEvent` arrives for ticket type "general" with capacity 100 on event "DevConf"
- **THEN** a capacity record for "general" is created in the Registrations module with max capacity 100 and 0 used

#### Scenario: Capacity record created with null capacity on TicketTypeAddedModuleEvent
- **WHEN** a `TicketTypeAddedModuleEvent` arrives for ticket type "speaker" with null capacity on event "DevConf"
- **THEN** a capacity record for "speaker" is created with null max capacity and 0 used

#### Scenario: Capacity ceiling updated on TicketTypeCapacityChangedModuleEvent
- **WHEN** a `TicketTypeCapacityChangedModuleEvent` arrives for ticket type "general" changing capacity from 100 to 200 on event "DevConf"
- **THEN** the max capacity in the Registrations module capacity record for "general" is updated to 200

#### Scenario: Capacity ceiling set to null on TicketTypeCapacityChangedModuleEvent
- **WHEN** a `TicketTypeCapacityChangedModuleEvent` arrives for ticket type "general" removing the capacity on event "DevConf"
- **THEN** the max capacity in the Registrations module capacity record for "general" is set to null
