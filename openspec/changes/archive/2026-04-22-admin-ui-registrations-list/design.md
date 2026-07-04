## Context

Admins create registrations via three paths (self-service, coupon redemption, admin-add) but the Admin UI exposes no list view. The Registrations module already has all data in `Registrations.registrations` (PostgreSQL); the missing pieces are a list query/endpoint and a UI page.

The page is the second slice of the new `admin-ui-registrations` capability (the first slice — "Add registration" — was just shipped in `2026-04-22-admin-add-registration`).

Constraints:
- Modular monolith conventions (ADR-001/002): keep the use case inside `Admitto.Module.Registrations`; expose via an admin endpoint under `/admin/teams/{teamSlug}/events/{eventSlug}`.
- Authorization via `RequireTeamMembership(Organizer)`, consistent with other admin endpoints in the module.
- The Admin UI uses the BFF + generated `@hey-api/openapi-ts` SDK pattern (see `2026-04-22-admin-add-registration` archive).
- Per user direction: load all registrations once and apply search/filter/sort/paging client-side (no server-side paging in this slice).

## Goals / Non-Goals

**Goals:**
- Operators can quickly find a specific registration on a given event by typing in a search box.
- Operators can scan the list, filter by ticket type, and sort by columns.
- Page remains responsive for events up to a few thousand registrations on a single fetch.
- Backend exposes a single, simple read endpoint reusable by CLI and future UI features.

**Non-Goals:**
- Server-side paging, search, or filtering.
- Bulk actions / multi-select rows.
- Editing or cancelling registrations from the list.
- Showing additional-detail fields as columns.
- Exporting CSV (button is a placeholder that surfaces "Coming soon").
- Status semantics other than the implied "Confirmed" — there is no Pending/Cancelled state on the Registration aggregate yet.

## Decisions

### D1. Single-page payload (client-side search/filter/page)
Per user direction. The BFF returns the full list as JSON; the page component does all filtering and pagination in memory.

- **Rationale**: Simplicity. Admin events are typically tens-to-low-thousands of registrations; client-side handling keeps the UI snappy and avoids URL state for paging/filters.
- **Trade-off**: A 5,000-registration event = a few hundred KB JSON payload. Acceptable for an admin-only page; revisit when an event approaches that size.
- **Alternative considered**: Server-side paging with query parameters. Rejected because the user explicitly asked for client-side and we have no current evidence of large-event pain.

### D2. List DTO shape
`RegistrationListItemDto { Guid Id, string Email, IReadOnlyList<TicketSummaryDto> Tickets, DateTimeOffset CreatedAt }` where `TicketSummaryDto { string Slug, string Name }`.

- `Email` already exists on the aggregate; expose as raw string.
- `Tickets` from `TicketTypeSnapshot` (slug + name); a registration may include multiple tickets, mirroring the domain.
- `CreatedAt` from the aggregate's audit columns (`CreatedAt` is set by EF / shared kernel base type — verify in handler implementation; if absent, derive from a database column or add via the ORM).
- `Status` and `Reconfirm` are intentionally absent from the DTO — the UI hardcodes them. Adding them now would imply behavior we don't yet have on the aggregate.

### D3. Endpoint path & auth
`GET /admin/teams/{teamSlug}/events/{eventSlug}/registrations`, `RequireTeamMembership(Organizer)`. Returns `200` with `RegistrationListItemDto[]`. Returns `404` when the team or event is unknown (mirroring sibling endpoints).

- **Rationale**: Consistent with the existing admin route layout from `admin-add-registration` (`POST /admin/teams/.../registrations`) and `coupon-management`.

### D4. Capacity for the summary tile
Reuse the existing event details endpoint (`GET /admin/teams/.../events/{slug}`) and ticket-types endpoint to compute total capacity. The summary line shows `Total: <registration count> of <capacity>` when at least one ticket type defines `MaxCapacity`; otherwise just `Total: <count>`.

- **Rationale**: Avoids a new aggregation endpoint; the front-end already fetches both DTOs for related pages.
- **Trade-off**: Two extra HTTP round-trips on initial load. Acceptable; both are already cached by react-query elsewhere.

### D5. Sorting / paging defaults
- Default sort: `Registered` descending (newest first).
- Sortable columns: Attendee (email), Ticket (first ticket name), Registered.
- Paging: 25 rows/page, with `« Prev` / `Next »` controls and "Showing X–Y of N".

### D6. CLI parity
Add `admitto event registration list --team <slug> --event <slug>` printing a compact table (email, tickets, registered). Required by the `cli-admin-parity` capability for every admin endpoint.

## Risks / Trade-offs

- **Large events** → Single-fetch payload could grow large (multi-MB) for events with 10k+ registrations. Mitigation: monitor; introduce server-side paging in a follow-up change once needed.
- **No name field** → The Attendee column shows email local-part as a stand-in. Mitigation: documented in the spec; once attendee profile data exists we can extend the DTO.
- **Hardcoded "Confirmed" / "—"** → If/when Cancellation or Reconfirm features ship, both columns will need real backing data and the spec must be updated. Mitigation: keep the columns visible (so the page layout doesn't shift later) but call out the placeholder explicitly in the spec.
- **CSV export button is non-functional** → Could confuse operators. Mitigation: button shows a clear "Coming soon" toast/dialog and remains disabled-looking.
