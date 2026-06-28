# ADR-014: Email-Owned Event Context Projection

## Status

Accepted

## Context

Email composition needs slow-changing team and event facts such as accent color, event name, public links, time zone, reconfirm policy, and lifecycle state. Reading these synchronously from Organization and Registrations during email work couples Email to sibling module internals and makes Registrations assemble Email-specific rendering context.

## Decision

Email owns a durable `email.event_email_context_view` projection keyed by `(team_id, ticketed_event_id)`. It is an application read model (`EventEmailContextView`) exposed through the module read store (`IEmailReadStore`) and maintained by a single role-based `EventEmailContextProjector` that writes the projection through the read store, matching the Registrations `ActivityLogView`/`ActivityLogProjector` convention (each persisted `DbSet` lives on exactly one store abstraction; projections live on the read store). Organization and Registrations publish integration events with the facts Email needs, and the projector idempotently upserts partial projection rows and re-issues reconfirm triggers when schedule-affecting facts change. Reusable reads are dedicated query slices (`GetEventEmailRenderingContext`, `GetActiveReconfirmTriggerSpecs`). Transactional email, built-in bulk rendering, and reconfirm scheduling read this projection through those query slices. Live Registrations facade reads remain for attendee-source recipient resolution and reconfirm candidate eligibility.

## Consequences

- Email rendering context is eventually consistent with recent team/event edits.
- Email no longer calls Organization for team branding while preparing application email.
- Projection handlers must tolerate duplicate and out-of-order integration events.
- Registration correctness, capacity, authorization, and attendee eligibility remain owned by Registrations/Organization, not by the Email projection.
