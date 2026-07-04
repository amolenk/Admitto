## Context

The registrations list page (`/teams/{teamSlug}/events/{eventSlug}/registrations`) gives admins a table view of all attendees for an event but provides no way to drill into a single registration. Admins need to inspect full attendee details (including additional detail fields), understand the attendee's lifecycle (registered, reconfirmed, cancelled), see which emails were sent, and — in the near future — act on the registration (cancel, change ticket types). The current `RegistrationListItemDto` intentionally omits additional details and email history to keep the list lightweight.

Cross-module boundaries require special care: email history lives exclusively in the Email module's `email_log` table, so it cannot be queried directly from the Registrations module.

## Goals / Non-Goals

**Goals:**
- Expose full registration detail (including activity log entries) via a new admin endpoint in the Registrations module
- Expose per-attendee email history via a new admin endpoint in the Email module using the `registrationId` column on `EmailLog`
- Add `registrationId` (nullable) to `EmailLog` so emails can be looked up per-registration without cross-module calls
- Introduce an `ActivityLog` projection in the Registrations module to store immutable lifecycle milestones projected from domain events
- Add a new Admin UI page that renders attendee details, activity timeline (from ActivityLog), tickets, and email history
- Include the full cancel workflow on the attendee detail page; remove the cancel action from the registrations table rows
- Make registrations table rows into clickable links to the attendee detail page
- Provide a placeholder "Change ticket types" button (no implementation)

**Non-Goals:**
- Implementing change ticket type actions (separate future change)
- Full event-sourcing / audit-log infrastructure beyond the targeted ActivityLog projection
- Pagination or infinite scroll for the emails section (volume is expected to be low per attendee)
- Real-time updates on the attendee page

## Decisions

### D1 — Two separate backend endpoints, two frontend fetches

**Decision:** Keep the registration detail and email history as separate endpoints (`GET …/registrations/{registrationId}` in the Registrations module and `GET …/registrations/{registrationId}/emails` in the Email module). The UI fetches both in parallel with `Promise.all` / React Query.

**Rationale:** A single combined endpoint would require the Registrations module to depend on the Email module (or vice versa), violating the rule that cross-module reads go through a facade, not direct composition inside a slice handler. Separate endpoints keep modules independent and composable. The small extra HTTP round-trip is invisible to the user since fetches are parallel.

**Alternatives considered:**
- _Single endpoint orchestrated by the API layer_: The API layer (`Admitto.Api`) could compose both modules, but adding orchestration logic there goes against the vertical-slice pattern where endpoints delegate entirely to one module.
- _Embed emails in the registration detail endpoint via IEmailFacade_: Possible but would require adding a new `IEmailFacade` contract to `Admitto.Module.Email.Contracts`, which is heavier than needed for this read-only view.

### D2 — `registrationId` added to `EmailLog`; email lookup uses it directly

**Decision:** Add a nullable `registration_id` column to `email_log`. All sends initiated by a registration-scoped event (single-send: `AttendeeRegistered`, `RegistrationCancelled`; bulk fan-out: rows where `BulkEmailRecipient.RegistrationId` is non-null) populate this column. The `GetAttendeeEmailsHandler` queries `email_log` by `(ticketedEventId, registrationId)` — no facade call required.

**Rationale:** A direct FK-style column is the simplest and most reliable lookup path. It eliminates the need for a new `IRegistrationsFacade` method and an extra round-trip to resolve the email address. It also makes the email log data richer for future analytics use cases. The column is nullable so existing rows and external-list bulk sends (which have no associated registration) are unaffected.

**Alternatives considered:**
- _Resolve recipient email via IRegistrationsFacade (original D2)_: Works but requires an extra cross-module call per request and adds a new method to the facade contract. Dropped in favour of the column approach.
- _Pass email address as query parameter from the UI_: Would expose PII in the URL and require the UI to make two sequential requests (get registration → get emails). Worse than either other option.

### D3 — `ActivityLog` projection replaces synthetic timeline

**Decision:** Introduce a new `ActivityLog` entity and table in the Registrations module. Three domain event handlers project immutable entries into it: `AttendeeRegisteredDomainEvent` → `Registered`, `RegistrationReconfirmedDomainEvent` → `Reconfirmed`, `RegistrationCancelledDomainEvent` → `Cancelled`. Each entry stores `RegistrationId`, `ActivityType`, `OccurredAt`, and optional metadata (e.g. `CancellationReason`). The `GetRegistrationDetailsHandler` fetches entries for the registration and includes them in the response DTO.

**Rationale:** A future change allows a cancelled registration to become active again via re-registration (keeping the same registration ID for QR code continuity and email uniqueness). In that scenario the `Registration` entity will cycle through multiple states, and the synthetic-timeline approach (reading current fields like `ReconfirmedAt`) would lose history from prior cycles. The ActivityLog as an append-only projection survives state resets and accurately records the full lifecycle regardless of how many cycles a registration goes through.

**Alternatives considered:**
- _Synthetic timeline from current Registration fields (original D3)_: Simpler for the current iteration but breaks as soon as the registration can be re-activated. Dropped.
- _Full event-sourcing of the entire Registration aggregate_: Provides complete auditability but is heavy infrastructure that goes well beyond the scope of this feature. The ActivityLog is a targeted projection of only the three milestones that the UI needs.

### D4 — Slug-based endpoint path remains consistent

**Decision:** Both new endpoints follow the same path hierarchy as the existing endpoints: `GET /admin/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}` and `GET /admin/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}/emails`. The ID segment is the raw GUID of the registration.

**Rationale:** Keeps URL patterns predictable and consistent with how cancellation is already routed (`POST .../registrations/{registrationId}/cancel`).

### D5 — Ticket types shown from the registration's snapshot, not re-resolved from catalog

**Decision:** The attendee detail page shows the ticket types from the registration's stored `Tickets` collection (i.e., the `TicketTypeSnapshot` slugs and names at registration time), not from the live catalog.

**Rationale:** Ticket display names can change after registration. Showing the snapshot preserves historical accuracy and is consistent with how the existing list endpoint works.

## Risks / Trade-offs

- **`registrationId` null for older email_log rows**: Rows created before this migration will have `registration_id = null`. The attendee emails endpoint will show no emails for registrations that predate the migration. → Acceptable trade-off; a backfill migration could be added later if needed.
- **Bulk emails without a `RegistrationId`** (external-list bulk sends): These log rows will have `registration_id = null` and will not appear on the attendee detail page. → Correct behaviour; external-list recipients are not registrations.
- **Cancel moved from table to detail page**: Admins who used to cancel directly from the list must now navigate to the detail page. This is intentional (cancel is a destructive action; requiring navigation is a minor friction that reduces accidental cancels).
