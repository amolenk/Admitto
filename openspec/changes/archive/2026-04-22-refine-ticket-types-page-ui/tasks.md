## 1. Header

- [x] 1.1 In `ticket-types/page.tsx`, fetch `TicketedEventDetailsDto` via `apiClient.get('/api/teams/${teamSlug}/events/${eventSlug}')` using react-query (mirroring `settings/layout.tsx`).
- [x] 1.2 Replace the hard-coded "Tickets" heading with `event?.name ?? eventSlug`.
- [x] 1.3 Update the summary line from "X sold ..." to "X registered of Y across K ticket types".

## 2. Card content & wording

- [x] 2.1 Change the "On sale" `Badge` text to "Available".
- [x] 2.2 Rename the "Sold" stat label on each card to "Registered".
- [x] 2.3 Update the "X% sold" caption beneath the capacity bar to "X% registered".
- [x] 2.4 Remove the slug paragraph (`<p className="text-xs text-muted-foreground font-mono">{t.slug}</p>`) from the card header.
- [x] 2.5 Rename the overflow-menu destructive item from "Cancel ticket type" — confirm it already says "Cancel ticket type" (was "Cancel sales" in the footer; the dropdown item already used "Cancel ticket type"). If any other surface (toast, confirm dialog, aria-label) says "Cancel sales", update it.

## 3. Card actions

- [x] 3.1 Delete the entire `{!t.isCancelled && (<div className="border-t px-5 py-3 ...">...</div>)}` footer block that renders the inline Edit / Cancel sales buttons.
- [x] 3.2 Verify the `…` dropdown still offers Edit and Cancel ticket type for active ticket types and nothing for cancelled ones.

## 4. Ticket-stub styling

- [x] 4.1 Add a `.ticket-card` (or similar) utility in `globals.css` that applies a stronger border-radius and a horizontal `border-dashed` divider near the bottom of the card. Use existing CSS variables for colour.
- [x] 4.2 Apply the new class to the outer `<Card>` in `TicketTypeCard`.
- [x] 4.3 Ensure the divider sits visually between the stat block and the card bottom edge for both capped and uncapped (no progress bar) variants.

## 5. Verification

- [x] 5.1 Run `cd src/Admitto.UI.Admin && pnpm build` and resolve any TypeScript errors.
- [x] 5.2 Manual smoke test against a running AppHost: header shows event name; card uses "Available", "Registered", no slug, no footer buttons; `…` menu still works; perforated divider visible; cancel action label reads "Cancel ticket type".
