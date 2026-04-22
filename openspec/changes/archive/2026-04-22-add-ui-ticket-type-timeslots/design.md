## Context

Time slots are slug-tagged labels (e.g. `morning`, `afternoon`, `track-a`) attached to a ticket type. The Registrations module uses them at registration time to refuse a single registration that picks two ticket types whose time slots overlap (`SelfRegisterAttendeeHandler.cs`, `Errors.OverlappingTimeSlots`). The data model and admin API already support them:

- `Domain/ValueObjects/TimeSlot.cs` — `record TimeSlot(Slug Slug)`.
- `Domain/Entities/TicketType.cs` — `TimeSlotSlugs : string[]`, set at construction.
- `AddTicketTypeHttpRequest.cs` — accepts `string[]? TimeSlots = null`.
- `TicketTypeDto` — exposes `string[] TimeSlots`.

The Admin UI's add form (`add-ticket-type-form.tsx`) currently hard-codes `timeSlots: null`, and neither the card view (`page.tsx`) nor the edit dialog displays them. There is no backend gap; this is purely a UI gap.

## Goals / Non-Goals

**Goals:**
- Let organizers set zero-or-more time-slot slugs when adding a ticket type.
- Show existing time-slot slugs from the same event as picker suggestions, so spelling stays consistent within the event.
- Display the time slots of each ticket type on the ticket-types page and (read-only) in the edit dialog.
- Validate input client-side against the same slug rules already used for ticket-type slugs (`^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$`).

**Non-Goals:**
- Editing the time-slot list of an existing ticket type. The backend `UpdateTicketType` does not support it and we do not extend it in this change.
- Introducing a first-class "TimeSlot" admin entity, dedicated CRUD, schedule/calendar view, or per-time-slot capacity.
- CLI changes (CLI already accepts `--time-slot` on `ticket-type add` and is unaffected).
- Backend, contract, or generated-SDK changes.

## Decisions

### Decision: Reuse the existing `AddTicketType` admin API as-is
The HTTP request, validator, and command already accept `TimeSlots`. The UI just needs to send the array instead of `null`. No backend change avoids extra migration / contract-test surface.

**Alternatives considered:** Adding a dedicated `SetTicketTypeTimeSlots` endpoint — rejected because it is unnecessary for create-only scope and would duplicate validation.

### Decision: Tag-style input with autocomplete from existing event time slots
The form uses a simple text input that converts an entered token (on Enter, comma, or blur) into a chip. Suggestions are derived client-side from `ticketTypes.flatMap(t => t.timeSlots)`, deduplicated. Free entry is always allowed; suggestions are a convenience to keep slugs consistent.

**Alternatives considered:**
- Plain comma-separated `<input>` — rejected: no suggestions and harder to validate per-token.
- A dedicated "manage event time slots" screen — rejected as out-of-scope and would require a backend list endpoint.

### Decision: Slug validation is client-side only, mirroring backend rules
The backend `AddTicketTypeValidator` already enforces slug rules and surfaces field errors per element (`TimeSlots[0]`…). The UI re-validates on entry to fail fast, and surfaces server-side errors via the existing `useCustomForm` / `apiClient` error mapping if validation slips through.

### Decision: Display time slots as compact chips on the ticket-type card
Shown beneath the slug line in the card header, using the existing `Badge` component (variant `outline`). When empty, no chip row is rendered (no "—" placeholder), to keep cards uncluttered for events that don't use time slots.

### Decision: Edit dialog shows time slots read-only
Renders the current chips (disabled styling) with a short helper text ("Time slots can't be changed after creation"). Keeps the UI honest about the backend constraint without removing the information.

## Risks / Trade-offs

- **Risk**: Users may expect to edit time slots after creation → Mitigation: explicit helper text in the edit dialog; follow-up change can extend the backend update endpoint when there is real demand.
- **Risk**: Free-form slug entry can produce typos that defeat the overlap check (e.g. `morning` vs `Morning`) → Mitigation: client-side slug regex normalises casing/format, and the suggestion list nudges users toward existing slugs in the event.
- **Trade-off**: No paging / search on suggestions (we just show the deduped list). Acceptable because realistic events have ≤ ~10 time slots.
