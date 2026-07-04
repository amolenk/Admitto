# Public Event Links Specification

## Purpose

Public event links use Admitto-owned URLs built from a configured public tickets base URL and the event's globally unique public slug.

## Requirements

### Requirement: Public event links use Admitto-owned slugs

The system SHALL expose anonymous Admitto-owned public event links under a configured public tickets base URL and a globally unique event slug. The canonical event link SHALL be `/e/{publicSlug}`. The route SHALL resolve the slug to exactly one active or archived ticketed event and SHALL NOT accept an arbitrary redirect target from query string or request body.

#### Scenario: Event slug redirects to event website

- **WHEN** a visitor opens `https://tickets.admitto.org/e/azure-fest-2026` and an event with public slug `azure-fest-2026` exists
- **THEN** the system redirects to the event's configured website URL

#### Scenario: Unknown slug returns not found

- **WHEN** a visitor opens `/e/unknown-event` and no ticketed event has that public slug
- **THEN** the system returns a not-found response and does not redirect

#### Scenario: Route is not an open redirect

- **WHEN** a visitor opens `/e/azure-fest-2026?redirect=https://attacker.example`
- **THEN** the redirect parameter is ignored or rejected and the system uses only the stored event website URL for the resolved event

---

### Requirement: Registration-specific email links use the public event slug

The system SHALL generate attendee-facing registration links in emails from the configured public tickets base URL and `TicketedEvent.PublicSlug`, rather than using arbitrary event-owned URLs as the visible primary CTA domain. Registration-specific links SHALL include the event slug and the registration identifier needed by the existing public flow.

#### Scenario: Ticket email uses Admitto-owned QR link

- **WHEN** a ticket email is prepared for registration `R1` on event slug `azure-fest-2026`
- **THEN** the QR-code link starts with `https://tickets.admitto.org/e/azure-fest-2026/qr-code/`

#### Scenario: Ticket email uses Admitto-owned cancel link

- **WHEN** a ticket email is prepared for registration `R1` on event slug `azure-fest-2026`
- **THEN** the cancellation link starts with `https://tickets.admitto.org/e/azure-fest-2026/cancel/`

---

### Requirement: Direct public event links redirect to website-relative actions

The system SHALL expose anonymous direct public event links under `/e/{publicSlug}` for registration, cancellation, and edit flows. Each route SHALL resolve `{publicSlug}` to a stored ticketed event and redirect to an action path appended to the event's configured website URL. If the configured website URL contains a path, the action path SHALL be appended to that path.

#### Scenario: Register link redirects to website register path

- **WHEN** a visitor opens `/e/azure-fest-2026/register` and the event website URL is `https://partner.example/events/azure-fest`
- **THEN** the system redirects to `https://partner.example/events/azure-fest/register`

#### Scenario: Cancel link redirects with registration ID

- **WHEN** a visitor opens `/e/azure-fest-2026/cancel/11111111-1111-1111-1111-111111111111` and the event website URL is `https://partner.example/events/azure-fest`
- **THEN** the system redirects to `https://partner.example/events/azure-fest/cancel/11111111-1111-1111-1111-111111111111`

#### Scenario: Edit link redirects with registration ID

- **WHEN** a visitor opens `/e/azure-fest-2026/edit/11111111-1111-1111-1111-111111111111` and the event website URL is `https://partner.example/events/azure-fest/`
- **THEN** the system redirects to `https://partner.example/events/azure-fest/edit/11111111-1111-1111-1111-111111111111`

#### Scenario: Unknown slug for action link returns not found

- **WHEN** a visitor opens `/e/unknown-event/register` and no ticketed event has that public slug
- **THEN** the system returns a not-found response and does not redirect
