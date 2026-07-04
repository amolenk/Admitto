## Context

The reconfirmation feature lets organizers set up a policy that periodically emails unconfirmed attendees. The original auto-cancel design (v1) added `AutoCancelEnabled` and `MaxReconfirmAttempts` to the event-level `TicketedEventReconfirmPolicy` — one threshold for all attendees of that event. This proved too coarse: a two-day conference may have limited-seat workshop tickets on day one and open sessions on day two, requiring auto-cancel only for workshop registrations.

The revised design (v2) moves the configuration to the **ticket type** level and derives a per-registration effective threshold from the attendee's booked ticket types at tick time.

The existing flow (unchanged):
1. `TicketedEventReconfirmPolicy` on `TicketedEvent` stores window, cadence, and `MinEmailInterval`.
2. The Email module drives a Quartz trigger per event that fires `EvaluateReconfirmJob`.
3. Each tick queries `IRegistrationsFacade` for unconfirmed registrations, queries the email log, applies `MinEmailInterval`, creates a `BulkEmailJob`.

What v1 added that **must be reverted**:
- `AutoCancelEnabled` and `MaxReconfirmAttempts` on `TicketedEventReconfirmPolicy`.
- Those fields in `ReconfirmTriggerSpecDto` and `TicketedEventReconfirmPolicyChangedIntegrationEvent` payload.
- Those constants as Quartz trigger job data keys.
- Auto-cancel toggle and number input in the Reconfirmation Policy form.

What v1 added that **stays**:
- `ReconfirmAutoCancel` in `RegistrationCancellationReason`.
- `ReconfirmAutoExpiredIntegrationEvent` (Email → Registrations) and its handler.
- `reconfirm-cancelled` email template type and built-in default.
- Email handler routing `Reason=ReconfirmAutoCancel` → `reconfirm-cancelled` template.
- The candidate-split logic in `EvaluateReconfirmJob` (needs adjustment but not a rewrite).

## Goals / Non-Goals

**Goals:**
- Allow organizers to mark individual ticket types as "auto-cancel eligible" by setting an optional `MaxReconfirmAttempts` on the ticket type.
- A registration is eligible for auto-cancel if it includes at least one ticket of such a type.
- The effective threshold is the **minimum** `MaxReconfirmAttempts` across all the attendee's eligible ticket types.
- At tick time, the Email module reads the per-registration effective threshold from the facade result — no additional cross-module reads needed.
- Expose the new field in the Admin UI ticket type form.

**Non-Goals:**
- Snapshotting `MaxReconfirmAttempts` at registration time — the live ticket type value is used at tick time (organizer intent prevails).
- Per-attendee attempt counts in the admin UI (out of scope).
- Any change to the cancellation logic in the Registrations module beyond what v1 already implemented.

## Decisions

### 1. `MaxReconfirmAttempts` is a property of `TicketType`, not `TicketedEventReconfirmPolicy`

**Decision:** Add an optional `MaxReconfirmAttempts: int?` (positive integer, nullable) to the `TicketType` entity. Remove `AutoCancelEnabled` and `MaxReconfirmAttempts` from `TicketedEventReconfirmPolicy`.

**Rationale:** Different ticket types within the same event have different seating constraints. Workshops are limited-seat; general sessions are open. Attaching the threshold to the ticket type makes auto-cancel eligibility a per-ticket-type concern rather than an event-wide switch. The `AutoCancelEnabled` boolean becomes implicit: a ticket type has auto-cancel when `MaxReconfirmAttempts` is non-null.

### 2. The facade computes `EffectiveMaxReconfirmAttempts` per registration via live ticket type lookup

**Decision:** Extend `RegistrationListItemDto` (in the `Registrations.Contracts` namespace) with `EffectiveMaxReconfirmAttempts: int?`. The facade implementation computes this by joining the registration's ticket type IDs against the live `TicketType.MaxReconfirmAttempts` values and taking the minimum of the non-null ones. `null` means this registration is not eligible for auto-cancel.

**Rationale:** The facade is the authorised cross-module read point. It already performs the join between registrations and ticket types for `TicketTypeIds`; extending that query to also compute the minimum `MaxReconfirmAttempts` is a natural extension. Using live values (not snapshots) means organizers can enable or disable auto-cancel for a ticket type at any time and the change takes effect on the next tick.

**Alternative considered:** Snapshot `MaxReconfirmAttempts` into `TicketTypeSnapshot` at registration time. Rejected because it requires a migration for all existing snapshot records, and it would mean that changing the ticket type setting after registration has no effect — which is contrary to organizer intent.

### 3. Trigger job data no longer carries auto-cancel configuration

**Decision:** Revert `AutoCancelEnabledKey` and `MaxReconfirmAttemptsKey` from `RequestReconfirmationsJob` constants and from `ScheduleReconfirmationsHandler`. `ReconfirmTriggerSpecDto` loses `AutoCancelEnabled` and `MaxReconfirmAttempts`. The `TicketedEventReconfirmPolicyChangedIntegrationEvent` payload is reverted to remove those fields.

**Rationale:** The threshold is now per-registration (varies per attendee's ticket types) and is returned inline from the facade result. Storing a single event-wide threshold in the trigger job data no longer makes sense. The tick reads `EffectiveMaxReconfirmAttempts` from each `RegistrationListItemDto` returned by `QueryRegistrationsAsync`.

### 4. Tick split logic uses per-registration `EffectiveMaxReconfirmAttempts`

**Decision:** After the existing email-log query and `MinEmailInterval` filter, the tick partitions candidates using the per-registration threshold from the facade DTO:

- **Reconfirm set** — `EffectiveMaxReconfirmAttempts == null` OR `email_log_count < EffectiveMaxReconfirmAttempts`
- **Auto-cancel set** — `EffectiveMaxReconfirmAttempts != null` AND `email_log_count >= EffectiveMaxReconfirmAttempts`

A `ReconfirmAutoExpiredIntegrationEvent` is enqueued for any non-empty auto-cancel set — identical to v1 behaviour.

**Rationale:** The same split pattern from v1 is preserved; only the threshold source changes. Mixed registrations within one event are handled naturally: each is evaluated against its own threshold.

Example with workshops (`MaxReconfirmAttempts=2`) and open sessions (null):
- Workshop+session attendee, 2 emails sent: threshold=2, count≥2 → auto-cancel
- Session-only attendee, 2 emails sent: threshold=null → reconfirm email (never auto-cancelled)

### 5. All other v1 additions carry forward unchanged

`ReconfirmAutoCancel` enum value, `ReconfirmAutoExpiredIntegrationEvent` and its Registrations handler, `reconfirm-cancelled` template and email routing — all keep exactly as implemented in v1. No changes needed.

## Risks / Trade-offs

- **Risk: Changing `TicketType.MaxReconfirmAttempts` after registrations exist takes effect immediately on the next tick.**  
  → Acceptable. Live-value semantics are the design intent. Organizers should be aware. Documentation / UI hint can clarify.

- **Risk: `QueryRegistrationsAsync` now joins ticket types — query cost increases slightly.**  
  → Mitigation: This is already a filtered query per event. Adding a JOIN on the ticket-catalog table is low cost. No N+1 risk; the minimum is computed in-query (single SQL pass with GROUP BY).

- **Risk: `RegistrationListItemDto` is a shared contract DTO used by other callers (bulk email, badge export, etc.).**  
  → Mitigation: The new field is nullable with default `null`. Existing callers that don't use it are unaffected.

## Migration Plan

1. **Revert** the v1 migration that added `auto_cancel_enabled` and `max_reconfirm_attempts` to the reconfirm policy storage.
2. **Add** a new migration: `max_reconfirm_attempts` (int, nullable) column on the ticket types table.
3. Deploy is non-destructive; existing ticket types default to `null` (no auto-cancel).
4. Rollback: column ignored; all registrations default to `null` effective threshold.
