## Why

Organizers currently have no way to automatically clean up registrations from attendees who repeatedly ignore reconfirmation prompts. Without this, unresponsive registrations occupy seats indefinitely even when the reconfirmation window closes.

However, a single event often has ticket types with very different seating constraints. A two-day conference may offer limited-seat workshops on day one alongside open-access sessions on day two. Auto-cancellation should apply only to registrations that include at least one capacity-constrained ticket type — not uniformly to all attendees regardless of what they booked.

## What Changes

- Add an optional `MaxReconfirmAttempts` positive integer field to `TicketType`. When set, this ticket type is "limited" and its value contributes to the auto-cancel threshold for any registration that includes it.
- An attendee's registration is eligible for auto-cancellation if they hold at least one ticket of a type with `MaxReconfirmAttempts` set. The effective threshold is the **minimum** `MaxReconfirmAttempts` across all their "limited" ticket types.
- When the email log count for an attendee reaches their effective threshold, the entire registration is cancelled (not just the limited-seat ticket).
- The `IRegistrationsFacade` is extended to include the per-registration effective `MaxReconfirmAttempts` (nullable) in its unconfirmed-registration result. The Email module uses this to determine eligibility and threshold at tick time — no cross-module direct reads needed.
- **Remove** `AutoCancelEnabled` and `MaxReconfirmAttempts` from `TicketedEventReconfirmPolicy` (v1 of this feature added them there — this change reverts that and moves the config to ticket type level).
- Add a `reconfirm-cancelled` email template type (with built-in default) for the auto-cancel notification.
- Expose `MaxReconfirmAttempts` in the Admin UI ticket type form (add) and remove the auto-cancel fields from the Reconfirmation Policy form (revert v1 UI).

## Capabilities

### New Capabilities

- `reconfirm-auto-cancel`: Domain and application logic that auto-cancels registrations at the scheduler tick when the email log shows the attendee's effective `MaxReconfirmAttempts` threshold has been reached. Covers the tick-time split (using per-registration threshold from facade), the cancellation trigger (`ReconfirmAutoExpiredIntegrationEvent`), and the domain `Cancel(ReconfirmAutoCancel)` operation.

### Modified Capabilities

- `ticket-type-management`: `TicketType` gains an optional `MaxReconfirmAttempts` (positive int, nullable). The `IRegistrationsFacade` unconfirmed-registration result is extended with `EffectiveMaxReconfirmAttempts: int?` (minimum across the attendee's tickets that have it set; null if none do).
- `event-management`: Revert `AutoCancelEnabled` and `MaxReconfirmAttempts` from `TicketedEventReconfirmPolicy` (added in v1 of this change). The policy returns to its pre-v1 shape: `Window`, `Cadence`, `MinEmailInterval` only.
- `reconfirm-sending`: The scheduler tick reads `EffectiveMaxReconfirmAttempts` per registration from the facade result. Registrations where `EffectiveMaxReconfirmAttempts` is set and `email_log_count >= threshold` are auto-cancelled instead of re-emailed. The Quartz trigger job data no longer carries `AutoCancelEnabled`/`MaxReconfirmAttempts` (remove those fields added in v1).
- `email-templates`: New `reconfirm-cancelled` template type added for the auto-cancel notification email (carried forward from v1 — no further changes needed if already present).
- `email-sending`: Handles `RegistrationCancelledIntegrationEvent` with `Reason = ReconfirmAutoCancel` by sending the `reconfirm-cancelled` template (carried forward from v1 — no further changes needed if already present).
- `admin-ui-ticket-types`: The ticket type create/edit form gains a `MaxReconfirmAttempts` optional number input with a hint explaining its purpose.
- `admin-ui-event-policies`: Remove the "Auto-cancel unreconfirmed registrations" toggle and "Max reconfirmation attempts" input that were added to the Reconfirmation Policy form in v1.

## Impact

- `Admitto.Core` / `Registrations` module:
  - `TicketType` aggregate: add optional `MaxReconfirmAttempts` field.
  - `TicketedEventReconfirmPolicy` value object: remove `AutoCancelEnabled` and `MaxReconfirmAttempts` (revert v1).
  - `IRegistrationsFacade` / unconfirmed-registration DTO: add `EffectiveMaxReconfirmAttempts: int?`.
  - `ReconfirmAutoCancel` cancellation reason enum value: keep from v1.
  - `ReconfirmAutoExpiredIntegrationEventHandler`: keep from v1 (no change needed).
  - `TicketedEventReconfirmPolicyChanged` integration event: revert `AutoCancelEnabled`/`MaxReconfirmAttempts` payload fields.
  - `ReconfirmTriggerSpecDto`: revert `AutoCancelEnabled`/`MaxReconfirmAttempts` fields.
- `Admitto.Core` / `Email` module:
  - `EvaluateReconfirmJob`: read `EffectiveMaxReconfirmAttempts` from facade result (per-registration); use it for the auto-cancel split. Remove event-level trigger job data keys for `AutoCancelEnabled`/`MaxReconfirmAttempts`.
  - `ScheduleReconfirmationsHandler`: stop writing `AutoCancelEnabled`/`MaxReconfirmAttempts` into trigger job data.
  - `reconfirm-cancelled` template: keep from v1.
  - Email handler for `ReconfirmAutoCancel` reason: keep from v1.
- `Admitto.UI.Admin`:
  - Ticket type create/edit form: add `MaxReconfirmAttempts` optional field.
  - Reconfirmation Policy form: remove auto-cancel toggle and max-attempts input (revert v1 UI).
- Database:
  - Revert v1 migration that added `auto_cancel_enabled`/`max_reconfirm_attempts` to reconfirm policy storage.
  - New migration: add `max_reconfirm_attempts` (int, nullable) to ticket types storage.
- No new integration events or cancellation reasons beyond what v1 already added.
