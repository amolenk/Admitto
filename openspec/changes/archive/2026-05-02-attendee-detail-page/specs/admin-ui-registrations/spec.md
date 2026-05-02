## ADDED Requirements

### Requirement: Admin UI registrations table rows link to the attendee detail page

Each row in the registrations table SHALL be clickable and navigate the admin to the attendee detail page at `/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}`. The attendee name or email SHALL serve as the primary clickable element; the full row MAY additionally support click-through navigation.

#### Scenario: SC023 Clicking a registration row navigates to the attendee detail page

- **GIVEN** an organizer is on the registrations list page and the table has at least one row
- **WHEN** they click on the attendee name in a row
- **THEN** the browser navigates to the attendee detail page for that registration's ID

## MODIFIED Requirements

### Requirement: Admin UI displays a registrations list page for each event (MODIFIED — cancel action removed)

The Admin UI SHALL provide a page at `/teams/{teamSlug}/events/{eventSlug}/registrations` that loads all registrations of the event in a single fetch and renders them in a table.

The table SHALL no longer include a per-row "Cancel" action. Cancel functionality has moved to the attendee detail page. Scenarios SC019, SC020, SC021, and SC022 from the base spec are removed.
