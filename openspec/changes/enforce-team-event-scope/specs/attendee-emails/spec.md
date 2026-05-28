## ADDED Requirements

### Requirement: Attendee email log is scoped to the owning team
The query that retrieves the email log for an attendee (`GetAttendeeEmails`) SHALL filter results by both `teamId` and `eventId`. The `TeamId` is already accepted by the query but was previously unused; it SHALL now be applied as a filter condition.

#### Scenario: Email log scoped to the correct team and event
- **WHEN** an organizer of team "team-a" requests the email log for a registration on an event that belongs to "team-a"
- **THEN** only email log entries for that registration, event, and team are returned

#### Scenario: Email log request for event belonging to a different team returns empty result
- **WHEN** an organizer of team "team-a" requests the email log for a registration using an event ID that belongs to "team-b"
- **THEN** the response is an empty list (the team filter excludes the entries)
