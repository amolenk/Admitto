# ADR-016: Hourly Reconfirmation Evaluation

## Status

Accepted. Supersedes the organizer-configured cadence and per-event Quartz trigger design.

## Context

The earlier reconfirmation design gave each event an organizer-selected cadence and a Quartz trigger. That made schedule state proportional to the number of events and coupled routine email evaluation to trigger creation, replacement, and removal. Reconfirmation policy data is already projected into Email's event context.

## Decision

- Keep the optional reconfirmation policy owned by `TicketedEvent` in Registrations. It defines a half-open window (`[opensAt, closesAt)`), a minimum whole-hour interval between reminder emails, and optional event-local quiet hours.
- Email projects the schedule-affecting policy, event time zone, and lifecycle state. The Worker runs one recurring Quartz evaluation hourly and considers only enabled policies on Active events.
- The hourly evaluation skips events outside the half-open window or inside their optional quiet hours. For eligible registered, unreconfirmed attendees, it applies the per-attendee minimum email interval before creating routine reconfirmation work.
- The hourly evaluation remains the only routine reminder scheduler. A policy-close one-shot trigger may invoke the same evaluator at the exact exclusive close instant; it performs terminal cancellation only and never creates reminder work. This trigger is operational safety for non-hour close times, not an organizer-configured cadence or a per-event routine trigger.
- Organizers configure policy eligibility, not scheduler timing. Clearing the policy or archiving the event makes it ineligible for future routine evaluation.
- Remove legacy per-event reconfirmation triggers from the Quartz store during rollout/reconciliation. The hourly evaluation must not recreate a trigger for a cleared, missing, or archived event.

## Consequences

- Reconfirmation reminder scheduling has one fixed operational cadence and no per-event routine trigger lifecycle to reconcile. One-shot close triggers are maintained only for terminal policy evaluation and are idempotent with the hourly evaluator.
- Quiet hours defer routine evaluation and reminder creation in the event's local time; the next hourly pass can evaluate the event when quiet hours end.
- Projection and queue-delivery lag can delay recognition of policy or lifecycle changes, but the next hourly pass uses the latest available Email projection.
- Existing legacy trigger records require cleanup during migration; after cleanup, stale per-event triggers cannot resume routine reconfirmation work.
