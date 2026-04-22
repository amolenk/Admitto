## Why

Admins can create registrations (self-service, coupon, admin-add) but the Admin UI currently has no page that lists them. Operators cannot see who has signed up for an event, search by name/email, or filter by ticket type. This is a basic operational need — every other admin feature (ticket types, coupons, settings) already has a list view.

## What Changes

- Add a new admin HTTP endpoint `GET /admin/teams/{teamSlug}/events/{eventSlug}/registrations` that returns all registrations for an event in a single response (email, ticket type slugs+names, registered-at timestamp). Authorization: team membership (Organizer or higher).
- Add a registrations list page in the Admin UI at `/teams/{teamSlug}/events/{eventSlug}/registrations` that:
  - Loads the full list once and applies search/filter/sort/paging client-side.
  - Shows a header summary tile: `Total: <count> of <capacity>` (capacity from event details; omit "of <capacity>" when no capacity is set).
  - Renders a table with columns: **Attendee** (email local-part with full email beneath), **Ticket** (one or more badges per registration), **Status** (hardcoded "Confirmed"), **Reconfirm** (hardcoded "—"), **Registered** (relative timestamp).
  - Provides a search box (matches email substring) and a ticket-type filter dropdown sourced from the event's ticket catalog.
  - Provides client-side pagination (25 rows/page).
  - Has an "Add registration" button linking to the existing `add` page and an "Export CSV" button that opens a "Coming soon" notification.
- Wire the page into the event sidebar navigation if not already present.

Out of scope (explicit, per user notes): multi-select rows, bulk actions, Company column / additional-detail columns, Pending status, Confirmed/Pending/Cancelled summary tiles, status tabs, server-side paging, CSV export.

## Capabilities

### New Capabilities
- `registration-listing`: Backend query and admin endpoint for listing all registrations of a ticketed event.

### Modified Capabilities
- `admin-ui-registrations`: Add a new requirement for the registrations list page (the existing capability currently only covers the "Add registration" affordance).

## Impact

- **Backend**: New use case `Admin.GetRegistrations` in `Admitto.Module.Registrations` (handler + DTO + admin HTTP endpoint). New CLI command `event registration list` for parity (per `cli-admin-parity` capability).
- **Admin UI**: New page + BFF route. Reuses existing `apiClient`, `useCustomForm`/table primitives, and ticket-type fetch endpoint. SDK regeneration required after backend lands.
- **Tests**: Application test for handler, API test for endpoint authorization + payload shape.
- **No schema changes**, no new domain events, no new dependencies.
