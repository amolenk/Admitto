# Attendee Emails Specification

## Purpose

This capability covers listing emails sent to a specific attendee within a ticketed event for admin use. It provides the query, handler, DTO, and admin HTTP endpoint that surface the email history for one attendee by querying the Email module's `email_log` directly, using the `registration_id` column added to `email_log` as part of this change.

## Requirements

### Requirement: Admin can list emails sent to a specific attendee within an event

The system SHALL expose a query (owned by the Email module) that returns all email log entries where:
- `ticketed_event_id` matches the resolved event
- `registration_id` matches the supplied `registrationId`

No cross-module facade call is needed; the `registration_id` column in `email_log` is the direct lookup key.

The returned items SHALL include per entry:
- `id`: email log entry GUID
- `subject`: email subject line
- `emailType`: the email type string (e.g. "ticket", "reconfirm", "bulk")
- `status`: `Sent`, `Delivered`, `Bounced`, or `Failed`
- `sentAt`: nullable timestamp
- `bulkEmailJobId`: nullable GUID, present when the email was sent as part of a bulk fan-out

Results SHALL be ordered by `statusUpdatedAt` descending (most recent first).

#### Scenario: SC001 Returns emails sent to the attendee for the event

- **GIVEN** two emails were sent in the context of Alice's registration for event "DevConf": a "ticket" email at T1 and a "reconfirm" email at T2, both with `registration_id` set to Alice's registration GUID
- **WHEN** an admin requests emails for Alice's registration
- **THEN** the system returns two entries ordered most-recent first

#### Scenario: SC002 Returns empty list when no emails were sent

- **GIVEN** a registration exists but no `email_log` rows have `registration_id` matching that registration
- **WHEN** an admin requests emails for that registration
- **THEN** the system returns an empty list

#### Scenario: SC003 Excludes emails for other events

- **GIVEN** the same `registrationId` somehow appears in email_log rows for two different events (edge case)
- **WHEN** an admin requests emails for that registration in event A
- **THEN** only entries for event A are returned (filtered by `ticketed_event_id`)

#### Scenario: SC004 Excludes emails for other registrations

- **GIVEN** two registrations exist for the same event, each having received emails with their respective `registration_id` set
- **WHEN** an admin requests emails for the first registration
- **THEN** only entries whose `registration_id` matches the first registration are returned

#### Scenario: SC005 Returns 404 when registration ID does not exist in the event

- **WHEN** the supplied `registrationId` does not match any registration scoped to the team and event (verified via the scope resolver)
- **THEN** the system returns a not-found result

### Requirement: Attendee emails are exposed via an admin HTTP endpoint

The system SHALL expose `GET /admin/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}/emails`, restricted to authenticated members of the team.

#### Scenario: SC006 Authenticated organizer receives email list

- **GIVEN** a user is a member of the team with the Organizer role
- **WHEN** they call `GET /admin/teams/acme/events/devconf/registrations/{id}/emails`
- **THEN** the system returns 200 with the email history payload

#### Scenario: SC007 Non-member of the team is forbidden

- **GIVEN** a user is authenticated but not a member of the team
- **WHEN** they call the endpoint
- **THEN** the system returns 403

