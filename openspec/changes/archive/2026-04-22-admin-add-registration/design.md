## Context

`Admitto.Module.Registrations` already supports two ways to create a `Registration`:

- **Self-service** (`SelfRegisterAttendeeHandler`): enforces window, email-domain, and capacity (via `TicketCatalog.Claim(slugs, enforce: true)`), plus the active-status gate.
- **Coupon** (`RegisterWithCouponHandler`): bypasses window/domain/capacity and instead validates the coupon, then claims with `enforce: false`.

Both reuse `Registration.Create`, `AdditionalDetails.Validate`, and the shared `ValidateTicketTypeSelection` helper. The `TicketCatalog` aggregate's `Claim` method already supports the `enforce: false` overload that increments used capacity without rejecting at the limit; it still respects `TicketCatalog.EventStatus` as the concurrency safety net.

What's missing is an admin-initiated path: organizers comping speakers, late additions, or attendees who can't self-register. Today they either mint single-use coupons (clutter, audit noise) or skip recording the registration (breaks attendance lists).

## Goals / Non-Goals

**Goals**
- A first-class admin path to create a `Registration` for any attendee email + ticket selection on a given event.
- Reuse existing aggregates, value objects, and validation helpers — no new domain concepts.
- Match the surface conventions of other admin features in this module: command + handler + `AdminApi/` slice + admin endpoint wiring + CLI command + Admin UI affordance.
- Make the admin path discoverable in the Admin UI from the event's registrations area.

**Non-Goals**
- Bulk import / CSV upload (single-registration only in this change).
- Sending a different confirmation email or skipping confirmation. The standard registration-created flow runs.
- Editing existing registrations (separate concern; not part of this change).
- Allowing admins to register against Cancelled or Archived events.
- A new domain aggregate. `Registration` and `TicketCatalog` are sufficient.
- A new authorisation role; the existing team-admin authorisation policy applies.

## Decisions

### D1. New use case rather than a "mode" flag on existing handlers
**Choice**: Introduce `AdminRegisterAttendeeCommand` + `AdminRegisterAttendeeHandler` under `Application/UseCases/Registrations/AdminRegisterAttendee/`.

**Why**: The two existing handlers each have a tight, readable shape. Adding an `IsAdmin` flag would entangle three different policy combinations into one method and obscure the bypass rules. A dedicated handler keeps each path's policy story explicit and aligns with the feature-sliced layout other admin endpoints use.

**Alternative considered**: Extend `SelfRegisterAttendeeCommand` with an `AdminOverride` flag. Rejected — couples public and admin policy in one place, harder to test and to reason about authorisation.

### D2. Reuse `TicketCatalog.Claim(slugs, enforce: false)`
**Choice**: Use the same unenforced-claim path the coupon handler uses.

**Why**: It already implements the exact behaviour we want — increment used capacity, bypass the limit check, but still trip the `TicketCatalog.EventStatus` safety net for concurrent cancel/archive. No new aggregate method needed.

### D3. Validation that still runs
The handler will run, in this order:
1. Load `TicketedEvent`; reject `EventNotFound` / `EventNotActive`.
2. Load `TicketCatalog`; reject `NoTicketTypesConfigured` if absent.
3. `ValidateTicketTypeSelection` (duplicates, unknown, cancelled, overlapping time slots) — reused from `SelfRegisterAttendeeHandler`.
4. Build ticket snapshots.
5. `catalog.Claim(slugs, enforce: false)`, mapping `TicketCatalog.Errors.EventNotActive` → `Errors.EventNotActive`.
6. `AdditionalDetails.Validate` against the event's current schema.
7. `Registration.Create(...)` then add to the write store.

The duplicate-email guard is enforced at persistence time by the existing unique index on `(EventId, Email)` (same mechanism as the other paths). Surfaces as a `DbUpdateException` with a known constraint name — the API layer maps it to a 409 the same way.

### D4. HTTP surface
- Route: `POST /admin/teams/{teamSlug}/events/{eventSlug}/registrations`
- Body: `{ "email": string, "ticketTypeSlugs": string[], "additionalDetails": Record<string,string>? }`
- Response: `201 Created` with `{ "id": "<registrationId>" }` and a `Location` header pointing at the registration detail (if one exists; otherwise omit Location).
- Auth: existing team-admin policy (same as the surrounding admin endpoints in the module).
- Validation: a `FluentValidation` validator runs in the endpoint filter (per the repo convention for admin routes) — non-empty email (valid format), at least one ticket type slug, no empty slugs.

### D5. CLI command
Add a CLI command under `src/Admitto.Cli/Commands/...` that calls the new endpoint via the regenerated NSwag `ApiClient`. Per the cli-admin-parity capability, the same change must regenerate `ApiClient.g.cs` (`generate-api-client.sh`) and add the command — no hand-edits to the generated client.

Suggested shape (final naming chosen during implementation to fit existing CLI tree):
```
admitto event registration add -t <team> -e <event> --email <email> --ticket <slug> [--ticket <slug>] [--detail key=value ...]
```

### D6. Admin UI surface
- Add an "Add registration" button on the event's registrations page, opening a modal/page form that collects:
  - Email
  - Ticket type selection (multi-select, populated from the event's ticket catalog)
  - Additional details (rendered from the event's `AdditionalDetailSchema` like the public flow already does)
- On submit, call the new admin endpoint via the Admin UI's `apiClient` (not the generated SDK; per the established convention).
- On success: refresh the registrations list and toast/confirm.
- On 4xx: render server validation errors inline (duplicate email, validation failures, event not active, etc.).
- Reuse existing form / validation primitives from the surrounding admin UI features for visual consistency.

### D7. Confirmation email behaviour
Admin-added registrations follow the same post-create flow as self-service: the existing `RegistrationCreated` domain/module event triggers the standard outbox-driven confirmation email. No new branching. (If product later wants a "silent add" toggle, it can be a separate change.)

## Risks / Trade-offs

- **Capacity over-fill** → admins can push used capacity past the configured limit. This matches the existing coupon behaviour and is intentional, but the Admin UI ticket-type detail view should already display "used / capacity" so over-allocation is visible. Mitigation: optional follow-up to surface a warning in the add-registration form when a selected ticket type is at/above capacity.
- **Audit / provenance** → the resulting `Registration` aggregate today does not record *how* it was created (self / coupon / admin). Mitigation: out of scope here; if needed, address in a separate provenance change rather than slipping it in.
- **Duplicate-email race** → enforced by the unique index, not at policy time, so a concurrent admin add + self-registration could both pass the policy check and one would fail at save. Acceptable — the failing call surfaces the same conflict as elsewhere.
- **Authorisation drift** → if a future role split (e.g. "registration manager" vs "team admin") emerges, this endpoint will need to be revisited. For now it lives under the same admin policy used by the rest of the module's admin surface.

## Open Questions

- Should the Admin UI live as a modal on the existing registrations page or as a dedicated `/registrations/new` route? Recommend a dialog/modal for parity with other lightweight admin actions in the UI; the form component itself should be reusable if a route is added later.
