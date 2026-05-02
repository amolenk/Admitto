## MODIFIED Requirements

### Requirement: Team member can list team events
The system SHALL allow team members with Crew role or above to list all events
for their team. The list is served by the Registrations module. Archived events
SHALL be excluded from listings. Events in the `Pending` creation
state (not yet materialised in Registrations) SHALL NOT appear in this list;
they are discoverable through the creation-status endpoint instead.

This requirement applies equally to the admin API endpoint
(`GET /admin/teams/{teamSlug}/events`); both admin and non-admin callers receive
only non-archived events. There is no endpoint today that returns archived
events; a dedicated endpoint will be introduced if that need arises in the
future.

#### Scenario: List active events excludes archived
- **WHEN** a Crew member of team "acme" lists events and "conf-2026" (active), "meetup-q1" (cancelled), and "conf-2025" (archived) exist
- **THEN** "conf-2026" and "meetup-q1" are returned and "conf-2025" is not included

#### Scenario: Pending creations are not listed
- **WHEN** team "acme" has a pending creation request for slug "future-conf" and a materialised active event "conf-2026"
- **THEN** only "conf-2026" is returned by the events list

#### Scenario: Admin listing also excludes archived events
- **WHEN** an admin calls `GET /admin/teams/acme/events` and "conf-2026" (active), "meetup-q1" (cancelled), and "conf-2025" (archived) exist
- **THEN** "conf-2026" and "meetup-q1" are returned and "conf-2025" is not included
