## Why

Several rough edges on the Ticket Types page hurt clarity and brand polish: the page still says generic "Tickets" instead of the event name, the "Sold" terminology is wrong for a free-event ticketing product, the cards feel like generic tiles, two competing edit/cancel affordances clutter each card, the slug is exposed without need, and the cancel button label is ambiguous. Cleaning this up makes the page feel both more professional and more on-brand for Admitto.

## What Changes

- Replace the static "Tickets" page title with the actual event name (fetched alongside the ticket types).
- Replace "sold" / "Sold" wording with "registered" / "Registered" in the header summary and per-card stat label.
- Restyle ticket cards to evoke a physical ticket (subtle: stronger rounded corners + a perforated/dashed divider between header and footer area), without changing layout.
- Replace the "On sale" badge text with "Available".
- Remove the inline edit / cancel buttons on the card footer; keep only the `…` overflow menu as the single edit/cancel affordance.
- Stop displaying the ticket-type slug in the card.
- Rename the cancel action from "Cancel sales" to "Cancel ticket type".

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `admin-ui-event-management`: ticket types page header, card visuals, and action affordances are tightened up.

## Impact

- **Code**: `src/Admitto.UI.Admin/app/(dashboard)/teams/[teamSlug]/events/[eventSlug]/ticket-types/page.tsx` (header, card markup, action bar removal, slug removal); `globals.css` may gain a small "perforated divider" utility class, otherwise CSS is in-file via Tailwind.
- **API**: No backend changes. Adds one client-side fetch of the existing `GET /api/teams/{teamSlug}/events/{eventSlug}` endpoint to obtain the event name (already used by `settings/layout.tsx` and the sidebar).
- **Tests**: No automated UI tests exist for this page. Manual smoke test only.
