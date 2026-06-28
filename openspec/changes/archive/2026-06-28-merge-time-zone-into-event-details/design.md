## Context

The Registrations module owns the authoritative `TicketedEvent` aggregate, including event name, URLs, dates, public slug, time zone, lifecycle, and policies. The Admin UI currently presents time zone alongside general event details, but saving uses two mutation paths: one request for general details and a second request for time zone. The backend mirrors that split with a dedicated `UpdateTicketedEventTimeZone` use case and `TicketedEventTimeZoneChanged` domain/integration events.

Email consumes event context through its own projection. That projection already stores both general details and time zone, and reconfirm scheduling already derives Quartz triggers from the projected time zone and reconfirm policy. The only remaining split is the event shape used to update that projection.

The product is not live yet, so this change does not need backwards-compatible endpoints, event aliases, or temporary dual-publishing.

## Goals / Non-Goals

**Goals:**

- Treat time zone as part of the general ticketed-event details mutation in API, UI, domain events, and integration events.
- Remove the dedicated time-zone update endpoint, use case, domain event, and integration event.
- Ensure Email projection updates and reconfirm trigger rescheduling still occur when an event's time zone changes.
- Keep event details updates guarded by the existing active-event and optimistic-concurrency rules.
- Regenerate the Admin UI SDK and use generated API functions rather than hand-written API calls.
- Update OpenSpec and architecture docs so the documented contracts match the simplified model.

**Non-Goals:**

- Do not introduce a new persisted value object or database schema change; `TicketedEvent.TimeZone` and `EventEmailContextView.TimeZone` already exist.
- Do not keep the old endpoint or publish the old `TicketedEventTimeZoneChanged` event for compatibility.
- Do not change event creation semantics; creation continues to require a valid time zone.
- Do not change how date/time instants are stored; `TimeZone` remains the display and scheduling zone, not a reinterpretation of stored UTC instants.

## Decisions

### D1. `UpdateTicketedEventDetails` owns time-zone mutation

`UpdateTicketedEventDetailsHttpRequest` and `UpdateTicketedEventDetailsCommand` will include `TimeZone`. The handler will parse it as `TimeZoneId` and pass it to `TicketedEvent.UpdateDetails(...)` together with name, URLs, public slug, and dates.

Rationale: time zone is edited in the General tab and participates in the same optimistic-concurrency unit as the other details. A single request avoids sequential UI writes and version arithmetic.

Alternative considered: keep the dedicated endpoint but make the UI call it only when needed. This preserves the current conceptual split and keeps separate events, which is the complexity this change removes.

### D2. Details-changed events carry the full Email event context

`TicketedEventDetailsChangedDomainEvent` and `TicketedEventDetailsChangedIntegrationEvent` will carry `TimeZone` in addition to name, website URL, and public slug. The distinct `TicketedEventTimeZoneChangedDomainEvent` and `TicketedEventTimeZoneChangedIntegrationEvent` will be deleted.

Rationale: Email needs a coherent event-context snapshot for rendering and scheduling. Including time zone in the details event lets a single message update the projection for all general event context fields.

Alternative considered: emit both details-changed and time-zone-changed from `UpdateDetails(...)`. This would preserve compatibility, but the application is not live and dual-publishing would preserve redundant contracts.

### D3. Email reschedules from applied details changes

`EventEmailContextProjector` will update projected time zone when handling `TicketedEventDetailsChangedIntegrationEvent`. If the update applies after version checks, it will reissue reconfirm scheduling from the projection state, just as the old time-zone-specific handler did.

Rationale: time zone affects cron evaluation, so a successful projected details update can affect reconfirm scheduling. The projection already centralizes schedule context and can decide whether a complete schedule exists.

Alternative considered: make only time-zone changes trigger rescheduling by comparing old/new time zone inside the projection. This is slightly more optimized but adds state comparison complexity for little benefit; schedule upsert is already idempotent.

### D4. Remove the UI/BFF time-zone route and regenerate SDK

The Admin UI General settings form will send one details update request containing `timeZone`. The BFF `time-zone` route will be removed, and the generated SDK will be regenerated from the updated OpenAPI spec before UI code relies on the new contract.

Rationale: the repository requires generated SDK usage for backend contract changes. Removing the extra BFF route keeps the UI aligned with the backend API.

Alternative considered: keep a BFF-only compatibility route that forwards to the details endpoint. This would be dead compatibility code and is unnecessary before launch.

## Risks / Trade-offs

- Existing local clients calling the old time-zone endpoint will fail → Accept because the product is not live; regenerate first-party SDK and update Admin UI callers in the same change.
- Details-changed events become schedule-affecting even when only non-time-zone fields change → Mitigate with idempotent projection version checks and schedule upsert semantics.
- Consumers might assume `TicketedEventDetailsChangedIntegrationEvent` only changes rendering fields → Mitigate by updating specs, architecture docs, and tests to document time zone as part of event details.
- Updating public slug/name and time zone in one request means one invalid field rejects the whole details update → Accept because the General tab is a single coherent edit surface and validators already reject invalid detail fields atomically.

## Migration Plan

1. Update specs and tasks for the consolidated details contract.
2. Update Registrations domain, use case, endpoint registration, and event publishing.
3. Update Email projection handling and reconfirm scheduling tests.
4. Remove the old time-zone endpoint/use case and old time-zone events.
5. Start Aspire, fetch the updated OpenAPI document, regenerate the Admin UI SDK, and update UI/BFF code.
6. Update architecture docs and ADR references.
7. Run architecture tests first, then targeted backend and UI verification.

Rollback before launch is a normal code revert. No database rollback is required because no schema migration is planned.

## Open Questions

- None. The application is not live, so the old endpoint and old integration event do not need compatibility aliases.
