# Badge Export Specification

## Purpose

Organizers export a CSV file for a given badge type that can be used to drive badge printing. For *ticket-based* badge types, the export is derived from live registration data (first name, last name, email, ticket type name, and any additional detail values collected at registration). For *standalone* badge types, the export is derived from the stored badge instances (display name, notes).

## ADDED Requirements

### Requirement: Organizer can export a CSV for a ticket-based badge type
The system SHALL allow organizers (Owner or Organizer role) to download a CSV export for a ticket-based badge type. The export SHALL contain one row per unique registrant whose registration includes at least one ticket type from the badge type's `ticketTypeIds` list and whose registration status is `Registered` (cancelled registrations SHALL be excluded). Registrants who hold multiple ticket types that all map to the same badge type SHALL appear exactly once (deduplicated by `RegistrationId`). Each row SHALL include the registration's first name, last name, email address, and the values of any additional detail fields defined on the event (with empty string for fields not provided on that registration). The CSV header row SHALL always be present; columns SHALL follow the order: `FirstName`, `LastName`, `Email`, then one column per additional detail field key in the order defined by the event's additional detail schema.

#### Scenario: Export CSV for a ticket-based badge type with registrations
- **GIVEN** event "conf-2026" has a ticket-based badge type "Conference Badge" linked to ticket types "Workshop" and "Conference", and has 2 active registrations: ("Alice Smith", "alice@example.com", holds "Conference" ticket, additionalDetails={"dietary":"vegan"}) and ("Bob Jones", "bob@example.com", holds "Workshop" ticket, additionalDetails={}), with an additional detail schema defining a single field "dietary"
- **WHEN** an organizer requests the CSV export for "Conference Badge"
- **THEN** the response is a CSV file with header row `FirstName,LastName,Email,dietary` and two data rows — one for Alice with dietary="vegan" and one for Bob with dietary=""

#### Scenario: Attendee holding multiple linked ticket types appears only once
- **GIVEN** event "conf-2026" has a ticket-based badge type "Conference Badge" linked to ticket types "Workshop" and "Conference", and Alice holds both ticket types under the same registration
- **WHEN** an organizer requests the CSV export for "Conference Badge"
- **THEN** Alice appears exactly once in the CSV

#### Scenario: Cancelled registrations are excluded from the export
- **GIVEN** event "conf-2026" has a ticket-based badge type "Conference Badge" linked to "Workshop" and 1 active and 1 cancelled registration for that ticket type
- **WHEN** an organizer requests the CSV export for "Conference Badge"
- **THEN** only the active registration appears in the CSV

#### Scenario: Export CSV with no matching registrations returns header only
- **GIVEN** event "conf-2026" has a ticket-based badge type "VIP Badge" with no active registrations for any of its linked ticket types
- **WHEN** an organizer requests the CSV export for "VIP Badge"
- **THEN** the response is a CSV file containing only the header row

---

### Requirement: Organizer can export a CSV for a standalone badge type
The system SHALL allow organizers to download a CSV export for a standalone badge type. The export SHALL contain one row per badge instance. Each row SHALL include the instance's display name and notes (empty string if no notes). The CSV header row SHALL always be present; columns SHALL be: `DisplayName`, `Notes`.

#### Scenario: Export CSV for a standalone badge type with instances
- **GIVEN** event "conf-2026" has a standalone badge type "Guest Badge" with instances ("Jane Doe", "Speaker's partner") and ("John Smith", "")
- **WHEN** an organizer requests the CSV export for "Guest Badge"
- **THEN** the response is a CSV file with header `DisplayName,Notes` and two data rows, one per instance

#### Scenario: Export CSV for a standalone badge type with no instances returns header only
- **GIVEN** event "conf-2026" has a standalone badge type "Guest Badge" with no instances
- **WHEN** an organizer requests the CSV export for "Guest Badge"
- **THEN** the response is a CSV file containing only the header row

---

### Requirement: IRegistrationsFacade exposes a badge-export query
The system SHALL extend `IRegistrationsFacade` in `Admitto.Core.Module.Registrations.Contracts` with a method `QueryRegistrationsForBadgeExportAsync(eventId, ticketTypeIds)` where `ticketTypeIds` is `IReadOnlyList<TicketTypeId>`. The method SHALL return a deduplicated collection of `BadgeExportRegistrationDto` items — one per unique `RegistrationId` that has at least one ticket from the provided list and has `Status = Registered`. Each DTO SHALL include: `FirstName`, `LastName`, `Email`, and `AdditionalDetails` (as `IReadOnlyDictionary<string, string>`). Deduplication SHALL be performed inside the Registrations implementation so callers never receive duplicate rows.

#### Scenario: Facade returns only active registrations matching any of the ticket types
- **GIVEN** event {event-id} has registrations: Alice (Registered, holds ticket type A), Bob (Registered, holds ticket type B), Charlie (Cancelled, holds ticket type A), and Dave (Registered, holds ticket type C not in the query list)
- **WHEN** `QueryRegistrationsForBadgeExportAsync(eventId, [A, B])` is called
- **THEN** only Alice and Bob are returned

#### Scenario: Facade deduplicates a registrant holding multiple matching ticket types
- **GIVEN** Alice's registration holds both ticket type A and ticket type B, both of which are in the query list
- **WHEN** `QueryRegistrationsForBadgeExportAsync(eventId, [A, B])` is called
- **THEN** Alice appears exactly once in the result

#### Scenario: Facade includes additional detail values in the result
- **GIVEN** a registration matching the query has `AdditionalDetails = { "dietary": "vegan", "tshirt": "M" }`
- **WHEN** `QueryRegistrationsForBadgeExportAsync(eventId, ticketTypeIds)` is called
- **THEN** the returned DTO includes `AdditionalDetails = { "dietary": "vegan", "tshirt": "M" }`
