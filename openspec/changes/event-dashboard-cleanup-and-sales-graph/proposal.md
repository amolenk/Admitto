## Why

The event dashboard hero card has a "Copy link" button and the check-in card has a "Share link" button that add visual noise without delivering meaningful value at this stage. Additionally, the dashboard lacks any trend visibility: organizers cannot see how registrations are accumulating over time, which is the most actionable metric during an active registration period.

## What Changes

- Remove the "Copy link" button from the event hero card.
- Remove the "Share link" button from the check-in card.
- Add a ticket sales trend card to the event dashboard showing registrations over time as a line/area sparkline chart, using the existing `createdAt` field on `RegistrationListItemDto`.

## Capabilities

### New Capabilities

- `event-dashboard-sales-graph`: A trend card on the event dashboard that visualises daily ticket sales over the last 14 days (and since opening, if shorter) using registration `createdAt` timestamps from the existing registrations endpoint. No new backend endpoint required.

### Modified Capabilities

- `admin-ui-event-management`: Remove the "Copy link" shortcut button from the event hero card and the "Share link" shortcut button from the check-in card.

## Impact

- **UI only** — no backend changes required.
- Files affected:
  - `src/Admitto.UI.Admin/app/(dashboard)/teams/[teamId]/events/[eventId]/components/event-hero-card.tsx` — remove Copy button
  - `src/Admitto.UI.Admin/app/(dashboard)/teams/[teamId]/events/[eventId]/components/check-in-card.tsx` — remove Share link button
  - `src/Admitto.UI.Admin/app/(dashboard)/teams/[teamId]/events/[eventId]/page.tsx` — add sales graph card, fetch registrations
  - New component: `src/Admitto.UI.Admin/app/(dashboard)/teams/[teamId]/events/[eventId]/components/sales-trend-card.tsx`
- Recharts is already installed (`^2.15.3`); no new dependencies.
