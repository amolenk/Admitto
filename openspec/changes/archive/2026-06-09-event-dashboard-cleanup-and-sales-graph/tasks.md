## 1. Dashboard Cleanup

- [x] 1.1 Remove the "Copy link" `Button` and its `Copy` icon import from `event-hero-card.tsx`; remove the `flex flex-col items-end gap-2 shrink-0` wrapper div if it becomes empty
- [x] 1.2 Remove the "Share link" `Button` and its `Copy` icon import from `check-in-card.tsx`; keep the "Scanner" button; remove the `Copy` import if no longer used

## 2. Sales Trend Card Component

- [x] 2.1 Create `src/Admitto.UI.Admin/app/(dashboard)/teams/[teamId]/events/[eventId]/components/sales-trend-card.tsx` with a `SalesTrendCard` component that accepts `registrations: RegistrationListItemDto[] | undefined` and `isLoading: boolean` props
- [x] 2.2 Implement the 14-day bucketing logic: build an array of `{ date: string; count: number }` objects for the last 14 days, counting registrations whose `createdAt` falls within each day (local calendar day based on the browser timezone)
- [x] 2.3 Compute the week-over-week delta: sum days 1–7 vs days 8–14, calculate percentage difference, and expose an `ArrowUp`/`ArrowDown` badge with green/amber colouring
- [x] 2.4 Render a Recharts `AreaChart` (responsive, via `ResponsiveContainer`) with: a filled gradient area in `--primary` colour, a `Line`/`Area` stroke, no axes labels (minimalist sparkline), and a simple `Tooltip` showing date + count
- [x] 2.5 Add an empty-state branch: when all bucket counts are zero, render a "No registrations yet" placeholder (`text-muted-foreground`, centred) instead of the chart
- [x] 2.6 Add a loading-state branch: when `isLoading` is `true`, render a `Skeleton` of the same height as the card

## 3. Wire Up in Dashboard Page

- [x] 3.1 In `page.tsx`, add `SalesTrendCard` below the existing `TicketBreakdownCard` / `CheckInCard` grid, spanning full width: `<SalesTrendCard registrations={registrations.data} isLoading={registrations.isLoading} />`
- [x] 3.2 Add a `registrations` query in `page.tsx` using the existing `/api/teams/${teamId}/events/${eventId}/registrations` proxy route (same pattern as `fetchTicketTypes`); use query key `["registrations", teamId, eventId]`

## 4. Verify

- [x] 4.1 Run `pnpm build` (or `pnpm typecheck`) inside `src/Admitto.UI.Admin` to confirm no TypeScript errors
