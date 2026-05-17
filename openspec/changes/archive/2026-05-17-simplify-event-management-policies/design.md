## Context

The `TicketedEvent` domain model currently carries three configurable policies:
1. **Cancellation policy** — a `LateCancellationCutoff` datetime that classifies attendee cancellations as "late" or "on time". No code actually acts on this classification (no fees, no business rules downstream).
2. **Reconfirmation policy** — a window + cadence that drives a Quartz-scheduled bulk-email loop. The cadence controls how often the scheduler *ticks*, but there is no per-attendee email throttle, so a short cadence can spam an individual attendee.
3. **Registration policy** — unchanged by this change.

The `TicketedEvent` lifecycle also includes a `Cancelled` status (Active → Cancelled → Archived) that requires a separate cancel-event operation. Ticket types similarly have a cancel operation. The organizer's real workflow is: inform attendees via bulk email, then archive. The intermediate Cancelled state adds complexity without adding value.

## Goals / Non-Goals

**Goals:**
- Remove `TicketedEventCancellationPolicy` from the domain and all surfaces (API, UI).
- Remove the `Cancelled` lifecycle status for events; allow direct `Active → Archived` transition.
- Remove the `CancelTicketType` operation; ticket types are active until the event is archived.
- Add `MinEmailInterval` (positive integer, in hours) to `TicketedEventReconfirmPolicy`. A reconfirmation email is suppressed for a specific attendee if the later of (their registration time, their last reconfirmation email sent-at) is less than `MinEmailInterval` hours ago.
- Replace the late-cancellation policy guard for self-service cancellation with a hard-coded rule: reject if `now >= event.StartsAt`.

**Non-Goals:**
- Changing the registration policy.
- Admin-initiated cancellation of individual registrations (the admin cancel path is unaffected).
- Changing how `BulkEmailJob` fan-out or the email log work beyond filtering.
- Preserving the `TicketedEventCancelled` integration event for downstream consumers (Organization) — that event is removed; the `TicketedEventArchived` event already drives the same counters.

## Decisions

### D1 — MinEmailInterval semantics: `max(registrationTime, lastReconfirmSentAt) + interval ≤ now`

Two scenarios require throttling: (a) a repeat email to an already-prompted attendee, and (b) a first email to a recently registered attendee who registered just before the window opened. Using a single `MinEmailInterval` that applies to the later of `registration.CreatedAt` and `lastReconfirmEmailSentAt` covers both with one field.

**Alternative considered**: separate fields (`InitialDelay` + `ResendInterval`). Rejected because it doubles the configuration surface for a rarely-needed distinction.

### D2 — MinEmailInterval enforcement is in the Email module's reconfirm job

The per-attendee last-send check is done inside `EvaluateReconfirmJob` before creating the `BulkEmailJob`. The Email module already calls `IRegistrationsFacade.QueryRegistrationsAsync` to build the recipient list; adding `CreatedAt` to `RegistrationListItemDto` gives the job registration time for each attendee at no extra cross-module round-trip. Last-send times come from a single bulk query against the local `email_log` table (one query per tick, scoped to `reconfirm` rows for the event). The job then excludes any attendee where `max(registrationCreatedAt, lastReconfirmSentAt) + MinEmailInterval > now`.

**Alternative considered**: push the filter into `BulkEmailJob`'s fan-out. Rejected because at fan-out time the job has already been committed; filtering before job creation gives a cleaner audit record (job `RecipientCount` reflects actual intended recipients, not suppressed ones).

### D3 — Remove Cancelled event status; migrate existing Cancelled events to Archived

The `EventLifecycleStatus` enum becomes `{ Active, Archived }`. Any existing rows with status `Cancelled` in the database SHALL be migrated to `Archived` as a one-time data migration in the EF Core migration that removes the status.

**Risk**: any API client that parses the `status` field and handles `"cancelled"` will break. Since this is an internal admin API with a controlled client (Admin UI), this is acceptable as a coordinated breaking change.

### D4 — TicketCatalog.EventStatus also drops Cancelled

The `TicketCatalog` projection simplifies from `Active → Cancelled → Archived` to `Active → Archived`. The domain event `TicketedEventCancelled` is removed. A one-time migration mirrors the event status migration.

### D5 — Hard-coded post-start guard for self-service cancellation

The `SelfCancelRegistrationCommandHandler` will check `now >= ticketedEvent.StartsAt` (accessed through `ITicketedEventReadModel` or equivalent facade) and reject with a `BusinessRuleViolationException`. The `StartsAt` is already stored on `TicketedEvent` and is available to the Registrations module.

**Alternative considered**: keep a configurable cancellation deadline. Rejected per the product decision — the hard-coded guard (event start time) is sufficient.

## Risks / Trade-offs

| Risk | Mitigation |
|---|---|
| Existing `Cancelled` events lose that status on migration | All `Cancelled` events are migrated to `Archived` in the same EF migration — no data is lost, only the classification changes |
| `TicketedEventCancelled` integration event is removed — any future consumer would need to use `TicketedEventArchived` instead | Document this as a breaking change; audit all handlers before removing |
| `MinEmailInterval` query against `email_log` on every Quartz tick adds latency | The log query is scoped to a single event's `reconfirm` rows; with an index on `(event_id, email_type, sent_at)` this is a fast range scan |
| Organizers with existing ticket-type `Cancelled` rows lose that status | Remove the `Cancelled` state from ticket types; migrate any cancelled ticket types to effectively "soft-deleted / legacy" or simply mark them as active — **ask product owner** (see Open Questions) |

## Migration Plan

1. Run EF migration: update `ticketed_events.status` column — drop `Cancelled` variant; migrate existing `Cancelled` rows to `Archived`.
2. Run EF migration: update `ticket_catalog.event_status` — same migration of `Cancelled → Archived`.
3. Remove `TicketedEventCancelled` integration event handler in the Organization module and the Email module.
4. Deploy API and Admin UI together (coordinated release) since the `status` field values change.
5. No rollback without re-adding the `Cancelled` status — treat as a one-way migration.

## Open Questions

- **OQ1**: Ticket types in `Cancelled` state: since ticket type cancellation is being removed, existing DB rows with `ticket_type.status = Cancelled` should be migrated to `Active`? Or archived/hidden? Recommend migrating to `Active` (the event will be archived or active anyway) unless there's a strong reason to preserve the distinction.
- **OQ2**: Should `MinEmailInterval` have a minimum enforced value (e.g. at least 1 hour)? Recommend yes, to prevent accidental zero-interval configuration.
- **OQ3**: The unit for `MinEmailInterval` — hours vs. days. The user's example ("just registered, shouldn't immediately get an email") suggests sub-day granularity is useful. Recommend **hours** with a minimum of 1.
