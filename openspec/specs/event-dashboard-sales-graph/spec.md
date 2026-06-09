# Event Dashboard Sales Graph

## Purpose

Organizers need a compact view of recent registration momentum on the event dashboard.

## Requirements

### Requirement: Event dashboard displays a ticket sales trend card
The Admin UI event dashboard SHALL include a `SalesTrendCard` component that shows the number of new registrations per day over the last 14 days (or since the event's registration-open date if it is more recent). The card SHALL derive its data from the `createdAt` field of `RegistrationListItemDto` objects returned by the existing registrations endpoint. No new backend endpoint is required.

The card SHALL display:
- A label "Registrations - last 14 days" (or fewer days when the event is newer).
- The total count of registrations in the displayed period as a large number.
- A week-over-week delta badge (e.g., "+12% vs prior week") with a green up-arrow when positive and an amber down-arrow when negative.
- A responsive Recharts `AreaChart` sparkline showing daily registration counts.
- Date labels on the x-axis for the start and end of the range.

When there are zero registrations in the period the card SHALL render a "No registrations yet" placeholder instead of an empty chart.

While registrations data is loading the card SHALL render a skeleton placeholder of the same height.

#### Scenario: Trend card shows daily registrations for a 14-day window
- **WHEN** an organizer opens the event dashboard and registrations have been created over the past 14 days
- **THEN** the trend card shows daily buckets across the 14-day range, with the total count and a week-over-week delta badge

#### Scenario: Trend card shows positive delta badge
- **WHEN** the second week has more registrations than the first week
- **THEN** the delta badge shows a green up-arrow and a positive percentage

#### Scenario: Trend card shows negative delta badge
- **WHEN** the second week has fewer registrations than the first week
- **THEN** the delta badge shows an amber down-arrow and a negative percentage

#### Scenario: Zero registrations renders a placeholder
- **WHEN** an event has no registrations at all
- **THEN** the trend card shows "No registrations yet" and does not render a chart

#### Scenario: Loading state renders skeleton
- **WHEN** registration data is still being fetched
- **THEN** the trend card area is replaced with a skeleton of the same dimensions
