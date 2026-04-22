## Context

The Ticket Types page (`app/(dashboard)/teams/[teamSlug]/events/[eventSlug]/ticket-types/page.tsx`) was built early and accumulated a handful of inconsistencies relative to the rest of the admin UI:

- The page title is hard-coded to "Tickets" while other event pages (e.g. `settings/layout.tsx`) show the event name.
- "Sold" terminology is a leftover from when paid tickets were on the roadmap. Admitto only sells free tickets — `docs/arc42/03-context-and-scope.md` describes the system as a ticketing tool for small free events. The right verb is "register".
- Cards have both inline footer actions (`Edit`, `Cancel sales`) and a `…` dropdown that exposes the same two actions. Two-source-of-truth UI.
- The slug is shown under the name; users only need it when calling the API or CLI, which they don't do from this page.
- "Cancel sales" is misleading because nothing is being sold.
- Cards are visually neutral; the product is about tickets, so a subtle ticket-stub treatment is on-brand.

## Goals / Non-Goals

**Goals:**
- Header reflects the current event name.
- Stat language is consistent across the header summary and every card.
- One affordance for edit/cancel per card (the `…` menu).
- Cards have a light "ticket" feel without requiring custom illustrations.

**Non-Goals:**
- Any backend, CLI, or contract changes.
- Reworking the Add/Edit dialogs (out of scope; covered by their own forms).
- A full visual redesign of the page (grid, padding, typography stay as-is).
- Internationalisation of new strings (English-only as the rest of the UI today).

## Decisions

### Decision: Reuse `GET /api/teams/{team}/events/{event}` for the event name
The settings layout and sidebar already fetch `TicketedEventDetailsDto` via this endpoint. The ticket types page will issue the same query (cached by react-query under the same key shape if consistent — at minimum a sibling key) and use `event.name` as the page title, falling back to `eventSlug` while loading.

**Alternatives considered:** Reading the event name from a parent layout via context — rejected; no parent fetches it on this route, and adding context plumbing for one string is overkill.

### Decision: "Ticket-stub" styling stays inside the existing `Card` component
We add (a) a slightly larger border-radius on the outer card and (b) a single horizontal "perforated" divider between the stat block and the (now footer-less) bottom edge, implemented as a Tailwind utility (`border-dashed` + small inset notches via `::before/::after` pseudo-elements in `globals.css`). No SVG, no extra components, no layout shift.

**Alternatives considered:**
- Full ticket-stub illustration (notches, barcode) — rejected as too loud per user direction.
- New `<TicketCard>` component — rejected; same shadcn `Card` keeps consistency with the rest of the admin UI.

### Decision: Drop the footer action bar entirely
With both edit and cancel always reachable via the `…` menu, the footer adds noise and duplicates affordances. The card body fills the saved vertical space; no other content moves to the footer.

### Decision: Stop rendering the slug on cards
The slug remains visible in the Edit dialog (where it actually matters as the immutable identifier). On the listing it adds visual noise without clear value to organizers.

### Decision: Wording — "Available", "Registered", "Cancel ticket type"
- Badge "On sale" → "Available". Aligns with free-event semantics.
- Header summary "X sold of Y" → "X registered of Y". Per-card "Sold" stat label → "Registered".
- Action label "Cancel sales" → "Cancel ticket type". Matches the actual operation (ticket type is marked cancelled).

## Risks / Trade-offs

- **Risk**: Removing footer buttons reduces discoverability of edit/cancel for users with limited mouse precision → Mitigation: the `…` button is already the primary entry point on the card and is sized as a `Button size="sm"`.
- **Risk**: Hiding the slug on the listing makes copy-paste workflows harder for power users → Mitigation: slug remains in the Edit dialog and via API/CLI; this is a listing, not a debug surface.
- **Trade-off**: Custom CSS for the perforated divider is a small CSS surface area we now own → Trivial maintenance cost.
