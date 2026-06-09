# Admin UI — Event Waitlist Overview

## Purpose

Organizers need a consolidated view of all waitlists for an event without having to
navigate through individual ticket type cards.

## ADDED Requirements

### Requirement: Organizer can view all waitlists for an event from the sidebar

The admin UI SHALL provide a Waitlist page accessible from the event sidebar. The page
SHALL display a summary table with one row per ticket type that has `waitlistEnabled =
true`. Each row SHALL show the ticket type name, the total number of active waitlist
entries, the total number of pending notifications, and a link to the per-ticket-type
waitlist detail page. Ticket types with `waitlistEnabled = false` SHALL be excluded from
the table.

#### Scenario: Event has multiple waitlist-enabled ticket types

- **WHEN** an organizer opens the Waitlist page for "DevConf" which has two ticket
  types with `waitlistEnabled = true` ("General Admission" with 5 waiting and 1
  pending, "VIP" with 2 waiting and 0 pending)
- **THEN** the table shows two rows — "General Admission" (5 waiting, 1 pending) and
  "VIP" (2 waiting, 0 pending) — each with a link to the respective detail page

#### Scenario: Event has no waitlist-enabled ticket types

- **WHEN** an organizer opens the Waitlist page for an event where no ticket types have
  `waitlistEnabled = true`
- **THEN** the page shows an empty state message explaining that no waitlists are
  configured and suggesting enabling the waitlist on a ticket type

#### Scenario: Waitlist sidebar entry is always visible

- **WHEN** an organizer is viewing any page for an event
- **THEN** the "Waitlist" entry is visible in the event sidebar navigation between
  "Ticket types" and "Badges"

#### Scenario: Organizer navigates to per-ticket-type detail from overview

- **WHEN** an organizer clicks the link in a row of the waitlist overview table for
  "General Admission"
- **THEN** they are navigated to the per-ticket-type waitlist detail page for
  "General Admission"
