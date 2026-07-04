## Why

Organizers printing name badges for their events have no structured way to define badge layouts, link them to ticket types or custom guest lists, or export a consolidated print-ready CSV. This capability closes that gap and supports the full badge-printing workflow from event configuration to file export.

## What Changes

- Introduce a new **Badges module** (`Admitto.Core.Module.Badges`) following the established module structure (Domain / Application / Infrastructure / Contracts).
- Allow organizers to define **badge types** per event: either *ticket-based* (one badge rendered per registration of one or more chosen ticket types, with attendees deduplicated across those types) or *standalone* (a free-form list managed independently, e.g. "Guest", "Speaker's Partner").
- For standalone badge types, allow organizers to **add, edit, and delete badge instances** directly in the admin UI (e.g. to record that a speaker is bringing their spouse).
- Provide a **CSV export per badge type**: one row per badge instance, with all relevant registration fields (first name, last name, ticket type, additional detail values) included for ticket-based types, and the organizer-supplied fields for standalone types.
- Wire the new module's endpoints into the API host and expose management and export pages in the Admin UI.

## Capabilities

### New Capabilities

- `badge-type-management`: Define, update, and delete badge types for an event. A badge type is either *ticket-based* (references one or more `TicketTypeId`s from the Registrations module — attendees are deduplicated across all linked ticket types) or *standalone* (no ticket link). Names must be unique per event.
- `standalone-badge-instances`: Add, edit, and delete individual instances of standalone badge types. Each instance carries at minimum a display name and can carry arbitrary key/value fields.
- `badge-export`: Export a CSV for a selected badge type. Ticket-based exports are built from live registration data (first name, last name, additional details) across all linked ticket types, with registrants who hold multiple of those ticket types appearing only once. Standalone exports are built from the stored instances. Both include the badge type name and any relevant fields.

### Modified Capabilities

*(none)*

## Impact

- **New module**: `Admitto.Core.Module.Badges` with its own EF Core DbContext, tables (`badge_types`, `badge_instances`), and outbox support.
- **Cross-module reads**: Badge export handler reads registration data via `IRegistrationsFacade` (existing Registrations Contracts facade); badge type management reads ticket type names via `IRegistrationsFacade` or a lightweight Registrations query for display purposes.
- **Integration events consumed**: `TicketedEventCreated` and `TicketedEventArchived`/`TicketedEventCancelled` from Registrations — so the Badges module can scope badge types to valid events and guard mutations when the event is no longer active.
- **API**: New admin endpoints under `/admin/teams/{teamSlug}/events/{eventId}/badge-types` and `/admin/teams/{teamSlug}/events/{eventId}/badge-types/{badgeTypeId}/instances`.
- **Admin UI**: New badge management and export pages under the event detail section.
- **Architecture tests**: Updated to include the Badges module namespace in the allowed module list.
- **No breaking changes** to existing modules or public API contracts.
