## Context

Quiet hours currently live as two scalar properties on `TicketedEvent` and are edited through the Admin UI General settings form. The only runtime consumer is waitlist coupon issuance: emails are sent immediately, and the coupon claim expiry is shifted when issuance happens inside the event's quiet-hours window.

The Registrations module owns both `TicketedEvent` and waitlist behavior, so moving quiet hours into a `WaitlistPolicy` remains inside the same module boundary. Admin UI event policy pages already host registration and reconfirm policy forms, making them the natural location for event-wide waitlist offer timing.

## Goals / Non-Goals

**Goals:**

- Model quiet hours as an event-owned waitlist policy rather than general event details.
- Keep waitlist quiet hours event-wide and required, with default values of `22:00` and `08:00`.
- Move quiet-hours editing to the Policies tab in a dedicated Waitlist policy section.
- Preserve existing waitlist runtime behavior.
- Normalize the database schema so waitlist policy columns mirror the other `TicketedEvent` policy column naming.
- Keep write transaction boundaries endpoint-owned and handlers free of unit-of-work commits.
- Regenerate generated Admin UI API types/SDK after backend contract changes before updating UI calls.

**Non-Goals:**

- Per-ticket-type quiet hours.
- Delaying waitlist notification emails during quiet hours.
- Changing `ClaimWindowHours`, waitlist ranking, coupon redemption, or WaitlistMode behavior.
- Introducing organizer-removable waitlist policy state; the policy is always present with defaults.

## Decisions

### Required `TicketedEventWaitlistPolicy` value object

Add a required value object on `TicketedEvent`, for example `TicketedEventWaitlistPolicy`, containing `QuietHoursStart` and `QuietHoursEnd`. `TicketedEvent` initializes it to `22:00` / `08:00` during creation and exposes a policy mutator such as `ConfigureWaitlistPolicy(...)`.

Rationale: the policy is conceptually event-wide waitlist configuration, not event identity metadata. Keeping it required avoids null/default branching in coupon expiry calculation and preserves the current behavior for all existing events.

Alternative considered: keep scalar properties on `TicketedEvent` and only move the UI. That would improve placement but leave the domain model misleading.

### Normalize waitlist policy database columns

Map the value object's two properties to policy-prefixed columns on `ticketed_events`, mirroring the existing owned policy mappings:

- `waitlist_policy_quiet_hours_start`
- `waitlist_policy_quiet_hours_end`

Use `OwnsOne` mapping on `TicketedEvent.WaitlistPolicy`, consistent with `RegistrationPolicy` (`registration_policy_*`) and `ReconfirmPolicy` (`reconfirm_policy_*`). Keep the columns required with defaults of `22:00` and `08:00`.

Rationale: the product is not live yet, so this change should optimize the schema rather than carry forward misleading general-purpose column names. The database shape should communicate that quiet hours are part of waitlist policy.

Alternative considered: preserve `quiet_hours_start` and `quiet_hours_end` to avoid migration churn. That is unnecessary before launch and would leave the persistence model inconsistent with the other event policies.

### Separate waitlist-policy endpoint

Remove quiet hours from the general event details update request and add a policy-oriented admin endpoint, for example `PUT /admin/teams/{teamId}/events/{eventId}/waitlist-policy`, accepting `quietHoursStart`, `quietHoursEnd`, and `expectedVersion`.

Rationale: event settings writes should remain aligned with the form being edited. A dedicated endpoint also matches the existing registration-policy and reconfirm-policy slices.

Alternative considered: keep quiet hours in the general details update payload while displaying them on the Policies tab. That would couple the UI's policy form to the wrong backend use case and keep future concurrency/error handling awkward.

### Event details expose nested `waitlistPolicy`

Change event details from top-level `quietHoursStart` / `quietHoursEnd` fields to a required nested `waitlistPolicy` object.

Rationale: read contracts should match the domain model and make the policy grouping obvious to UI callers.

Alternative considered: expose both nested and top-level fields temporarily. This is unnecessary unless there are external consumers that require compatibility; the Admin UI SDK can be regenerated and updated with the backend change.

### Policies tab owns the UI

Add a Waitlist policy form to the existing event Policies tab. The form is read-only for archived events, uses the event's current version for optimistic concurrency, and refreshes event details after saving. Remove quiet-hours controls from the General form and its update payload.

Rationale: this keeps all event policy management in one place and fixes misleading General-form copy that implied notifications resume after quiet hours.

Alternative considered: add a separate Waitlist tab. The current change is small enough to fit the existing Policies tab without adding navigation complexity.

## Risks / Trade-offs

- API contract break for Admin UI generated types -> Regenerate the SDK after backend changes and update all UI references in the same change.
- EF owned value-object mapping changes the schema -> Treat this as a pre-live schema cleanup; no production data migration is required, but generated migrations/model snapshots should be reviewed for the intended policy-prefixed columns and defaults.
- Concurrent edits across multiple policy forms may conflict because all use `TicketedEvent.Version` -> Preserve existing optimistic concurrency behavior and surface the standard error.
- Naming could be confused with per-ticket waitlist settings -> Use copy that says the policy applies to all ticket-type waitlists for the event.

## Migration Plan

1. Add the domain value object and map it with `OwnsOne` to `waitlist_policy_quiet_hours_start` and `waitlist_policy_quiet_hours_end`.
2. Add the waitlist-policy command, request, validator, endpoint, and endpoint registration.
3. Update event details DTO and general event update request to use the new contract shape.
4. Generate the EF schema change through the official tooling; because the product is not live, no data-preserving migration from `quiet_hours_start` / `quiet_hours_end` is required.
5. Regenerate the Admin UI API SDK from the Aspire-backed OpenAPI spec.
6. Move the Admin UI controls from General to Policies and use the generated waitlist-policy call.
7. Run architecture tests first, then targeted domain/integration/UI checks.

Rollback before launch is to revert the code, generated SDK, and generated EF migration/model snapshot changes, then reset the local/dev database if needed.

## Open Questions

- Should the endpoint path be exactly `/waitlist-policy`, or should all event policy endpoints eventually be grouped under `/policies/*`? The minimal change should follow the existing `registration-policy` and `reconfirm-policy` route style.
