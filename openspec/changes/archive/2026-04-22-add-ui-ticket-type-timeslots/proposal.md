## Why

Time slots already exist in the Registrations domain (used to prevent overlapping ticket choices during attendee registration), and the backend `AddTicketType` admin endpoint accepts them. However the Admin UI silently sends `timeSlots: null` and never displays them, so organizers cannot use the feature without dropping to the CLI. Surfacing time slots in the UI closes that gap and unlocks multi-track / multi-session events.

## What Changes

- Add a "Time slots" input to the **Add ticket type** dialog in the Admin UI, allowing organizers to enter zero or more time-slot slugs.
- Suggest time-slot slugs already used by other ticket types on the same event (autocomplete / chip suggestions) so spelling stays consistent within an event.
- Display each ticket type's time slots on the ticket types page (card view) and as a non-editable read-out in the **Edit ticket type** dialog.
- The **Edit ticket type** flow remains capacity- and name-only; time slots are immutable after creation in this change. (Out of scope: backend `UpdateTicketType` is unchanged.)

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `admin-ui-event-management`: ticket-type creation form gains a time-slots field, and the ticket-types listing surfaces time slots per ticket type.

## Impact

- **Code**: `src/Admitto.UI.Admin/app/(dashboard)/teams/[teamSlug]/events/[eventSlug]/ticket-types/` (`add-ticket-type-form.tsx`, `edit-ticket-type-form.tsx`, `page.tsx`).
- **API**: No backend changes. Uses the existing `POST /admin/teams/{team}/events/{event}/ticket-types` endpoint (already accepts `timeSlots: string[]`) and the existing `GET …/ticket-types` response (already returns `timeSlots`).
- **Generated SDK**: No regeneration required (UI uses `apiClient` directly, the existing `TicketTypeDto` already exposes `timeSlots`).
- **Tests**: Manual smoke test of the dialog flow; no new automated tests required (UI-only change with no new server contract).
