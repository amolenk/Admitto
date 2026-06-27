## ADDED Requirements

### Requirement: TicketedEvent has a globally unique public slug
The `TicketedEvent` aggregate SHALL own a required `PublicSlug` used for Admitto-owned public links. The slug SHALL be globally unique across all ticketed events, SHALL be URL-safe, and SHALL be returned by event create/status, detail, and list responses where event identity fields are returned. Event `WebsiteUrl` and `BaseUrl` SHALL remain event-owned URL fields and SHALL NOT be replaced by the public slug.

#### Scenario: Create event with public slug
- **WHEN** an organizer requests creation of event "Azure Fest 2026" with public slug `azure-fest-2026`
- **THEN** the materialized `TicketedEvent` stores `PublicSlug = azure-fest-2026` alongside its website URL and base URL

#### Scenario: Reject duplicate public slug
- **WHEN** an organizer requests creation of an event with public slug `azure-fest-2026` and another event already uses that slug
- **THEN** the request is rejected with a conflict or validation error and no new `TicketedEvent` is materialized with that slug

#### Scenario: Update public slug
- **WHEN** an organizer updates an active event's public slug from `azure-fest-2026` to `azure-fest-eu-2026` with the correct version
- **THEN** the event stores the new public slug and its version is incremented

#### Scenario: Base URL remains available
- **WHEN** event details are queried for an event with public slug `azure-fest-2026` and base URL `https://azurefest.com`
- **THEN** the response includes both the public slug and the base URL
