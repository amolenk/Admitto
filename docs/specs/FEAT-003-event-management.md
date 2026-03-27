# Feature Specification: Event Management

## 1. Overview

| Field           | Value                                                   |
| --------------- | ------------------------------------------------------- |
| Feature ID      | FEAT-003                                                |
| Status          | Draft                                                   |
| Author          | Copilot + User                                          |
| Created         | 2026-03-26                                              |
| Last updated    | 2026-03-26                                              |
| Epic / Parent   | Organization Module                                     |
| Arc42 reference | 5. Building Block View — Organization module            |

### 1.1 Problem Statement

Organizers need to create and manage ticketed events within their teams, including
defining ticket types with capacities and time slots. Currently, only event creation
is implemented — there is no way to view event details, list a team's events, update
event information, cancel or archive events, or manage ticket types after the event
is created.

### 1.2 Goal

Organizers can create ticketed events, view and list their team's events, update
event details, cancel events that won't happen, and archive events that are over.
They can manage ticket types (add, update, cancel) within active events. Team
members with Crew role or above can view events and their ticket types.

### 1.3 Non-Goals

- Registration, reconfirm, or cancellation policies (separate feature)
- Capacity claiming and releasing (belongs to the Registrations module)
- Signing keys for URL security
- Additional detail schemas / custom fields
- Public-facing event discovery (external event sites use the Public API)
- Event deletion (events persist; cancel or archive instead)

## 2. User Stories

### US-001: Create a ticketed event

**As an** organizer,
**I want** to create a ticketed event for my team with a name, dates, and URLs,
**so that** I can start setting up ticket types and open registration.

### US-002: View event details

**As a** team member (Crew+),
**I want** to view a ticketed event's details and its ticket types,
**so that** I can see the event configuration.

### US-003: List team events

**As a** team member (Crew+),
**I want** to see all active events for my team,
**so that** I can find and manage upcoming events.

### US-004: Update event details

**As an** organizer,
**I want** to update an event's name, dates, or URLs,
**so that** the event information stays accurate.

### US-005: Cancel an event

**As an** organizer,
**I want** to cancel an event that won't take place,
**so that** it's clearly marked as cancelled and no further changes can be made.

### US-006: Archive an event

**As an** organizer,
**I want** to archive an event that is over or cancelled,
**so that** it's removed from active listings but preserved for historical reference.

### US-007: Add a ticket type

**As an** organizer,
**I want** to add a ticket type to an event with a name, capacity, time slots, and
self-service settings,
**so that** attendees can register for specific ticket options.

### US-008: Update a ticket type

**As an** organizer,
**I want** to update a ticket type's name, capacity, or self-service availability,
**so that** I can adjust ticket options as needed.

### US-009: Cancel a ticket type

**As an** organizer,
**I want** to cancel a ticket type that should no longer be available,
**so that** no new registrations can be made for it.

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                      | Priority | User Story |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------ | -------- | ---------- |
| FR-001 | The system shall allow organizers to create a ticketed event with a slug, name, website URL, base URL, and start/end dates.                      | Must     | US-001     |
| FR-002 | The system shall enforce unique event slugs within a team.                                                                                        | Must     | US-001     |
| FR-003 | The system shall validate that the event end date is on or after the start date.                                                                  | Must     | US-001     |
| FR-004 | The system shall allow team members (Crew+) to view a ticketed event's details and ticket types by event slug.                                   | Must     | US-002     |
| FR-005 | The system shall allow team members (Crew+) to list all events for their team.                                                                   | Must     | US-003     |
| FR-006 | The system shall exclude archived events from listings by default.                                                                                | Should   | US-003     |
| FR-007 | The system shall allow organizers to update an event's name, website URL, base URL, and start/end dates.                                         | Must     | US-004     |
| FR-008 | The system shall use optimistic concurrency (expected version) to prevent lost updates.                                                           | Must     | US-004     |
| FR-009 | The system shall prevent modifications to cancelled or archived events.                                                                           | Must     | US-004, US-005, US-006 |
| FR-010 | The system shall allow organizers to cancel an active event.                                                                                      | Must     | US-005     |
| FR-011 | When an event is cancelled, the system shall cancel all its active ticket types.                                                                  | Should   | US-005     |
| FR-012 | The system shall allow organizers to archive an active or cancelled event.                                                                        | Must     | US-006     |
| FR-013 | The system shall allow organizers to add a ticket type to an active event with a slug, name, self-service flag, time slots, and optional capacity. | Must     | US-007     |
| FR-014 | The system shall enforce unique ticket type slugs within an event.                                                                                | Must     | US-007     |
| FR-015 | The system shall allow organizers to update a ticket type's name, capacity, and self-service availability.                                        | Must     | US-008     |
| FR-016 | The system shall not allow changes to a ticket type's slug after creation.                                                                        | Must     | US-008     |
| FR-017 | The system shall allow organizers to cancel a ticket type, preventing new registrations for it.                                                   | Must     | US-009     |

## 4. Acceptance Scenarios

### SC-001: Successfully create a ticketed event (FR-001)

```gherkin
Given a team "acme" exists and is active
  And the requester is an organizer of team "acme"
When they create an event with slug "conf-2026", name "Acme Conf 2026",
  website "https://conf.acme.org", base URL "https://tickets.acme.org",
  starting 2026-06-01 and ending 2026-06-03
Then the event is created with the provided details
  And the event is in an active state with no ticket types
```

### SC-002: Reject duplicate event slug within a team (FR-002)

```gherkin
Given team "acme" already has an event with slug "conf-2026"
  And the requester is an organizer of team "acme"
When they create another event with slug "conf-2026"
Then the request is rejected with a duplicate slug error
```

### SC-003: Reject end date before start date (FR-003)

```gherkin
Given the requester is an organizer of team "acme"
When they create an event starting 2026-06-03 and ending 2026-06-01
Then the request is rejected with a validation error
```

### SC-004: View event details with ticket types (FR-004)

```gherkin
Given team "acme" has an event "conf-2026" with two ticket types
  And the requester is a Crew member of team "acme"
When they view event "conf-2026"
Then the event's name, dates, URLs, status, and both ticket types are returned
```

### SC-005: List active events excludes archived (FR-005, FR-006)

```gherkin
Given team "acme" has events "conf-2026" (active), "meetup-q1" (cancelled),
  and "conf-2025" (archived)
  And the requester is a Crew member of team "acme"
When they list events for team "acme"
Then "conf-2026" and "meetup-q1" are returned
  And "conf-2025" is not included
```

### SC-006: Update event details (FR-007, FR-008)

```gherkin
Given team "acme" has an active event "conf-2026" at version 1
  And the requester is an organizer of team "acme"
When they update the event name to "Acme Conference 2026" with expected version 1
Then the event name is changed
  And the version is incremented
```

### SC-007: Concurrent update conflict (FR-008)

```gherkin
Given team "acme" has an event "conf-2026" at version 2
  And the requester is an organizer of team "acme"
When they update the event with expected version 1
Then the request is rejected with a concurrency conflict error
```

### SC-008: Reject update of cancelled event (FR-009)

```gherkin
Given team "acme" has a cancelled event "meetup-q1"
  And the requester is an organizer of team "acme"
When they attempt to update the event name
Then the request is rejected because the event is cancelled
```

### SC-009: Cancel an active event (FR-010, FR-011)

```gherkin
Given team "acme" has an active event "conf-2026" with two active ticket types
  And the requester is an organizer of team "acme"
When they cancel event "conf-2026"
Then the event status is changed to cancelled
  And both ticket types are cancelled
```

### SC-010: Reject cancelling an already cancelled event (FR-009)

```gherkin
Given team "acme" has a cancelled event "meetup-q1"
  And the requester is an organizer of team "acme"
When they attempt to cancel event "meetup-q1"
Then the request is rejected because the event is already cancelled
```

### SC-011: Archive an active event (FR-012)

```gherkin
Given team "acme" has an active event "conf-2025"
  And the requester is an organizer of team "acme"
When they archive event "conf-2025"
Then the event status is changed to archived
```

### SC-012: Archive a cancelled event (FR-012)

```gherkin
Given team "acme" has a cancelled event "meetup-q1"
  And the requester is an organizer of team "acme"
When they archive event "meetup-q1"
Then the event status is changed to archived
```

### SC-013: Reject archiving an already archived event (FR-009)

```gherkin
Given team "acme" has an archived event "conf-2024"
  And the requester is an organizer of team "acme"
When they attempt to archive event "conf-2024"
Then the request is rejected because the event is already archived
```

### SC-014: Add a ticket type to an active event (FR-013)

```gherkin
Given team "acme" has an active event "conf-2026"
  And the requester is an organizer of team "acme"
When they add a ticket type with slug "vip", name "VIP Pass",
  self-service enabled, time slots ["morning", "afternoon"], and capacity 100
Then the event has a ticket type "vip" with the provided details
```

### SC-015: Reject duplicate ticket type slug (FR-014)

```gherkin
Given event "conf-2026" already has a ticket type with slug "vip"
  And the requester is an organizer of team "acme"
When they add another ticket type with slug "vip"
Then the request is rejected with a duplicate ticket type slug error
```

### SC-016: Reject adding ticket type to cancelled event (FR-009)

```gherkin
Given team "acme" has a cancelled event "meetup-q1"
  And the requester is an organizer of team "acme"
When they attempt to add a ticket type to event "meetup-q1"
Then the request is rejected because the event is cancelled
```

### SC-017: Update a ticket type's capacity and availability (FR-015)

```gherkin
Given event "conf-2026" has a ticket type "vip" with capacity 100
  And the requester is an organizer of team "acme"
When they update ticket type "vip" to capacity 200 and disable self-service availability
Then the ticket type capacity is changed to 200
  And self-service availability is disabled
```

### SC-018: Reject changing a ticket type's slug (FR-016)

```gherkin
Given event "conf-2026" has a ticket type "vip"
  And the requester is an organizer of team "acme"
When they attempt to change the slug of ticket type "vip" to "premium"
Then the request is rejected because ticket type slugs are immutable
```

### SC-019: Cancel a ticket type (FR-017)

```gherkin
Given event "conf-2026" has an active ticket type "vip"
  And the requester is an organizer of team "acme"
When they cancel ticket type "vip"
Then the ticket type is marked as cancelled
  And no new registrations can be made for it
```

### SC-020: Reject cancelling an already cancelled ticket type (FR-017)

```gherkin
Given event "conf-2026" has a cancelled ticket type "early-bird"
  And the requester is an organizer of team "acme"
When they attempt to cancel ticket type "early-bird"
Then the request is rejected because the ticket type is already cancelled
```

### SC-021: Crew member cannot create events (authorization)

```gherkin
Given the requester is a Crew member of team "acme"
When they attempt to create an event
Then the request is rejected as unauthorized
```

### SC-022: Non-member cannot view events (authorization)

```gherkin
Given the requester is not a member of team "acme"
When they attempt to view an event for team "acme"
Then the request is rejected as unauthorized
```

## 5. Domain Model

### 5.1 Entities

#### TicketedEvent (Aggregate Root) — _exists, needs extension_

_A scheduled event belonging to a team, with one or more ticket types defining
registration options._

| Attribute   | Type                   | Constraints                        | Description                          | Status    |
| ----------- | ---------------------- | ---------------------------------- | ------------------------------------ | --------- |
| id          | TicketedEventId (UUID) | PK, generated at creation          | Unique identity                      | ✅ Exists |
| teamId      | TeamId (UUID)          | required, FK to Team               | Owning team                          | ✅ Exists |
| slug        | Slug                   | required, unique within team       | URL-safe event identifier            | ✅ Exists |
| name        | DisplayName            | required, max 64 chars             | Event display name                   | ✅ Exists |
| websiteUrl  | AbsoluteUrl            | required, max 320 chars            | Event website URL                    | ✅ Exists |
| baseUrl     | AbsoluteUrl            | required, max 320 chars            | Base URL for ticketing               | ✅ Exists |
| eventWindow | TimeWindow             | required, end ≥ start              | Event start and end dates            | ✅ Exists |
| ticketTypes | list of TicketType     | owned, stored as JSON              | Ticket options for this event        | ✅ Exists |
| status      | EventStatus            | required, defaults to Active       | Active / Cancelled / Archived        | 🆕 New    |
| version     | uint                   | auto-incremented                   | Optimistic concurrency token         | ✅ Exists |

#### TicketType (Owned Value Object) — _exists, needs extension_

_A registration option within a ticketed event. Defines capacity, time slots,
and self-service availability._

| Attribute              | Type        | Constraints                          | Description                                     | Status    |
| ---------------------- | ----------- | ------------------------------------ | ----------------------------------------------- | --------- |
| slug                   | Slug        | required, unique within event, immutable | Ticket type identifier                        | ✅ Exists |
| name                   | DisplayName | required, max 64 chars               | Public display name                             | ✅ Exists |
| isSelfService          | boolean     | required                             | Whether this type supports self-service         | ✅ Exists |
| isSelfServiceAvailable | boolean     | required                             | Whether self-service is currently open          | ✅ Exists |
| timeSlots              | TimeSlot[]  | required                             | Named time slots for this ticket type           | ✅ Exists |
| capacity               | Capacity?   | nullable, 0–10,000                   | Optional max capacity; null means unlimited     | ✅ Exists |
| isCancelled            | boolean     | defaults to false                    | Whether this ticket type has been cancelled     | 🆕 New    |

### 5.2 Relationships

- A **Team** has many **TicketedEvents** (one-to-many, via teamId FK)
- A **TicketedEvent** has many **TicketTypes** (one-to-many, owned JSON collection)

### 5.3 Value Objects

_Most exist in the shared kernel or Organization domain._

| Value Object    | Attributes                           | Constraints            | Status    |
| --------------- | ------------------------------------ | ---------------------- | --------- |
| TicketedEventId | GUID                                 | Not empty              | ✅ Exists |
| EventStatus     | enum: Active, Cancelled, Archived    | —                      | 🆕 New    |
| TimeWindow      | start (DateTimeOffset), end (DateTimeOffset) | end ≥ start    | ✅ Exists |
| TimeSlot        | Slug                                 | Named slot identifier  | ✅ Exists |
| Capacity        | int                                  | 0–10,000               | ✅ Exists |
| Slug            | string                               | URL-safe, max 64 chars | ✅ Exists |
| DisplayName     | string                               | Non-empty, max 64 chars| ✅ Exists |
| AbsoluteUrl     | string                               | Valid absolute URI, max 320 chars | ✅ Exists |

### 5.4 Domain Rules and Invariants

- **Event end ≥ start**: The event window end date must be on or after the start
  date.
- **Unique event slug per team**: No two events within the same team may share a
  slug (enforced at database level via composite unique index).
- **Unique ticket type slug per event**: No two ticket types within the same event
  may share a slug.
- **Ticket type slug is immutable**: A ticket type's slug cannot be changed after
  creation.
- **Event lifecycle**:
  - Active → Cancelled (one-way; cancels all active ticket types)
  - Active → Archived (one-way)
  - Cancelled → Archived (one-way)
  - Archived is terminal (no further transitions)
- **Cancelled events block mutation**: No updates, no new ticket types on cancelled
  events.
- **Archived events block mutation**: No updates, no new ticket types, no
  cancellation on archived events.
- **Capacity range**: When specified, capacity must be between 0 and 10,000
  (inclusive).

## 6. Non-Functional Requirements

_Project-wide NFRs (JWT authentication, observability, error response format) apply
per arc42 Section 10 and are not repeated here._

| ID      | Category    | Requirement                                                                                           |
| ------- | ----------- | ----------------------------------------------------------------------------------------------------- |
| NFR-001 | Security    | Only organizers (or above, including admins) may create, update, cancel, or archive events and manage ticket types. |
| NFR-002 | Security    | Team members with Crew role or above may view event details and listings.                             |
| NFR-003 | Performance | Event listing shall respond within 500ms at p95 for a team with up to 100 events.                    |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                 | Expected Behavior                                                                 |
| ----- | -------------------------------------------------------- | --------------------------------------------------------------------------------- |
| EC-1  | Create event for an archived team                        | Return domain error; per FEAT-001 archived teams block mutations                  |
| EC-2  | Create event with duplicate slug in same team            | Return validation error; no event created                                         |
| EC-3  | Create event with end date before start date             | Return validation error                                                           |
| EC-4  | Update event so end date is before start date            | Return validation error; event unchanged                                          |
| EC-5  | Update event with stale version                          | Return concurrency conflict (409)                                                 |
| EC-6  | Update or add ticket types to a cancelled event          | Return domain error; event is cancelled                                           |
| EC-7  | Update or add ticket types to an archived event          | Return domain error; event is archived                                            |
| EC-8  | Add ticket type with duplicate slug in same event        | Return domain error; ticket types unchanged                                       |
| EC-9  | Cancel event with no ticket types                        | Succeeds; event is cancelled (no ticket types to cascade)                         |
| EC-10 | Cancel or archive an already-archived event              | Return domain error; event is already archived                                    |
| EC-11 | Set ticket type capacity to 0                            | Allowed; means no tickets available for this type                                 |
| EC-12 | Set ticket type capacity to null                         | Allowed; means unlimited capacity                                                 |
| EC-13 | Update a cancelled ticket type                           | Return domain error; ticket type is cancelled                                     |
| EC-14 | Concurrent ticket type additions to same event           | Serialized through event's optimistic concurrency; one succeeds, other conflicts  |

## 8. Success Criteria

| ID     | Criterion                                                                                   |
| ------ | ------------------------------------------------------------------------------------------- |
| SC-001 | All 22 acceptance scenarios pass in CI.                                                     |
| SC-002 | Event lifecycle transitions (Active → Cancelled → Archived) are enforced correctly.         |
| SC-003 | Ticket type operations are serialized through the event aggregate's concurrency token.      |
| SC-004 | Authorization enforces Organizer+ for mutations and Crew+ for reads on every endpoint.      |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **FEAT-001 Team Management**: Archived team guard prevents creating events for
  archived teams. Event archiving interacts with FEAT-001's team archive guard
  (a team with active events cannot be archived).
- Existing TicketedEvent aggregate and Organization module infrastructure
  (partially built).
- Shared kernel value objects (Slug, DisplayName, AbsoluteUrl, Capacity, TimeWindow).

### 9.2 Constraints

- Event slug is immutable after creation (used in URLs).
- Ticket types are owned value objects stored as JSON inside the event; all
  ticket type operations go through the TicketedEvent aggregate.
- The Registrations module consumes ticket type data via `IOrganizationFacade`
  — changes to the TicketType structure may require contract updates.

### 9.3 Architecture References

| Arc42 Section                    | Relevance to This Feature                                                      |
| -------------------------------- | ------------------------------------------------------------------------------ |
| 3. Context & Scope               | External event sites query ticket types via Public API                         |
| 5. Building Block View           | Organization module owns events and ticket types                               |
| 6. Runtime View                  | Endpoint → handler → write store flow; JSON-owned TicketType aggregation       |
| 8. Crosscutting Concepts         | Validation (FluentValidation), auth (Organizer/Crew policies), unit of work, error handling |
| 9. Architecture Decisions (ADRs) | ADR-001 (modular monolith), ADR-002 (feature-sliced endpoints)                 |
| 10. Quality Requirements         | Maintainability, reliability quality goals                                     |

## 10. Open Questions

_No open questions. All decisions resolved during specification._

| #   | Question                                                     | Owner | Status   | Resolution                                                             |
| --- | ------------------------------------------------------------ | ----- | -------- | ---------------------------------------------------------------------- |
| 1   | Should events be deletable?                                  | User  | Resolved | No — cancel or archive instead                                         |
| 2   | Should cancelled events be archivable?                       | User  | Resolved | Yes — Active → Cancelled → Archived or Active → Archived              |
| 3   | Should event slug be updatable?                              | User  | Resolved | No — slug is immutable (used in URLs)                                  |
| 4   | Should policies be in scope?                                 | User  | Resolved | No — registration/reconfirm/cancellation policies are a separate feature |

