## Context

The `RegisterAttendeeHandler` already handles three registration modes (SelfService, Admin, Coupon), sharing a single command and handler. The ticket-change operation needs similar structure: one command/handler with admin-only access initially, designed to allow a future self-service mode (matching the proposal note). Capacity management is already split into `ClaimWithEnforcement` (public) and `ClaimUncapped` (admin/coupon), and `Release(slugs)` is available on `TicketCatalog`. The `RegistrationCancelledDomainEvent` already triggers a capacity release via the `ReleaseTicketCapacityHandler`. The email flow runs via integration events consumed by the Email module.

## Goals / Non-Goals

**Goals:**
- Admin can replace the ticket-type selection on an existing `Registered` registration.
- Only the **delta** (added tickets) is claimed; released tickets are freed. A sold-out event must not block changing already-held tickets.
- All existing ticket-type selection validations apply (no duplicates, no unknown/cancelled types, no overlapping time slots).
- The change is recorded in the `activity_log` as a `TicketsChanged` entry.
- The attendee receives a new confirmation email (reusing the `ticket` template type).
- The `ticket` email template is extended with a `{{ ticket_types }}` variable so all confirmation emails list what the attendee holds.

**Non-Goals:**
- Self-service ticket change (deferred; structure prepared for it).
- Changing any other registration field (email, name, additional details).
- Changing tickets on a cancelled registration.

## Decisions

### D1 — Delta capacity management: release-then-claim on a derived set

The handler computes two sets: `toRelease` = current slugs ∖ new slugs, and `toClaim` = new slugs ∖ current slugs. It calls `catalog.Release(toRelease)` and then `catalog.Claim(toClaim, enforce: false)` (admin always unenforced). This is correct because:
- Slugs in both old and new sets need neither release nor re-claim — capacity is unchanged.
- Only genuinely removed slugs are freed; only genuinely new slugs consume capacity.
- A sold-out event does not block the change, because the admin path is unenforced and because already-held slugs are not re-claimed.

**Alternative considered**: Release all old, claim all new. Rejected — this falsely frees capacity for slots being retained, potentially allowing a concurrent public registration to slip in, then blocks the re-claim if capacity is now full (causing the "sold out" bug the user described).

### D2 — Fold ticket-selection validation into `TicketCatalog.Claim`

`TicketCatalog` already owns all the data needed to validate a ticket selection (slugs, `IsCancelled`, `TimeSlots`). Rather than adding a separate `ValidateSelection` method, the validation is folded directly into `Claim` so it is impossible to claim without validating. `Claim` will run, in order: duplicate-slug check, unknown-slug check, cancelled-slug check, overlapping-time-slot check, then the per-type capacity claim loop.

Error codes move from `RegisterAttendeeHandler.Errors` to `TicketCatalog.Errors` (using `ticket_catalog.*` prefix, consistent with the catalog's existing errors). The handler's four error factory methods (`DuplicateTicketTypes`, `UnknownTicketTypes`, `CancelledTicketTypes`, `OverlappingTimeSlots`) and the `ValidateTicketTypeSelection` private method are deleted. `Registration.EnsureNoDuplicateSlugs` (and its `Errors.DuplicateTicketTypes`) is also removed — the duplicate check in `Claim` is the single enforcement point.

Both `RegisterAttendeeHandler` and `ChangeAttendeeTicketsHandler` call `catalog.Claim(slugs, enforce: ...)` and get validation for free; neither handler needs to build a `ticketTypeMap` or call a separate validate step.

**Note on `Release`**: `Release` does not validate — it silently skips unknowns by design (unchanged).

### D3 — New domain event: `TicketsChangedDomainEvent`

A dedicated event carries `OldTickets` and `NewTickets` snapshots (slugs + names), the changed-at timestamp, and identity fields. This lets the activity log show a meaningful diff and lets the email handler know exactly which tickets to list. Reusing `AttendeeRegisteredDomainEvent` is rejected because the semantics differ (no new registration is created) and would cause idempotency-key clashes in the email handler.

### D4 — Integration event `AttendeeTicketsChangedIntegrationEvent` → `ticket` template type

A new integration event carries the full ticket snapshot for the email. The Email module reuses the `ticket` email template type (same design as the confirmation after initial registration) rather than introducing a new template type. This means organisers configure one template to cover both first-confirmation and ticket-change scenarios, which is simpler. A single `SendEmailCommand` is dispatched with idempotency key `tickets-changed:{registrationId}:{changedAt-unix-ms}`.

### D5 — `ticket_types` variable added to `ticket` template

Both the initial-registration email handler (`AttendeeRegisteredIntegrationEventHandler`) and the new ticket-change handler will pass a `ticket_types` list to the renderer. The built-in default templates are updated to display the list. Custom templates that don't use `{{ ticket_types }}` are unaffected (Scriban silently ignores unused variables).

### D6 — Single `ChangeAttendeeTicketsHandler` with a `ChangeMode` enum

Mirrors `RegistrationMode` on `RegisterAttendeeCommand`. Initially only `Admin` mode is defined; `SelfService` can be added later. The admin HTTP endpoint always constructs the command with `ChangeMode.Admin`.

### D7 — `Registration.ChangeTickets(...)` domain method

The aggregate owns the state mutation. It validates no duplicate slugs (same guard as `Create`), replaces `_tickets`, and raises `TicketsChangedDomainEvent`. Business logic for ticket-type validity (unknown/cancelled/overlapping) stays in the handler (matches pattern in `RegisterAttendeeHandler`).

## Risks / Trade-offs

- [Risk] Two concurrent ticket-change requests for the same registration could interleave capacity moves. → Mitigation: EF optimistic concurrency on `TicketCatalog` (existing `RowVersion`) will cause one to fail with a concurrency exception, retryable at the HTTP layer.
- [Risk] `ticket_types` variable added to existing email templates may display an empty list if the parameter is not passed (e.g. by a future handler). → Mitigation: The variable defaults to an empty list in Scriban; the template wraps the block in an `if` guard.
- [Risk] Changed tickets on a registration that was sent a reconfirm request: the existing signed reconfirm link still works (it references the registrationId, not the ticket snapshot). No action needed.

## Migration Plan

1. Deploy backend changes: new event, new handler, new endpoint, updated email handlers and templates.
2. No DB migration required — `activity_type` is stored as a string; `TicketsChanged` is new valid value.
3. Deploy Admin UI change: remove top "Change ticket types" button; wire "Change" button to new endpoint.
4. No rollback risk for existing data — the change is additive.

## Open Questions

- None blocking implementation.
