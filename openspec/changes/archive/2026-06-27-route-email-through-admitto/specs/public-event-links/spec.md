## ADDED Requirements

### Requirement: Public event links use Admitto-owned slugs
The system SHALL expose Admitto-owned public event links under a configured public tickets base URL and a globally unique event slug. The canonical event link SHALL be `/e/{publicSlug}`. The route SHALL resolve the slug to exactly one active or archived ticketed event and SHALL NOT accept an arbitrary redirect target from query string or request body.

#### Scenario: Event slug resolves to event
- **WHEN** a visitor opens `https://tickets.admitto.org/e/azure-fest-2026` and an event with public slug `azure-fest-2026` exists
- **THEN** the system resolves the slug to that event and either renders a controlled public event page or redirects to the event's configured URL

#### Scenario: Unknown slug returns not found
- **WHEN** a visitor opens `/e/unknown-event` and no ticketed event has that public slug
- **THEN** the system returns a not-found response and does not redirect

#### Scenario: Route is not an open redirect
- **WHEN** a visitor opens `/e/azure-fest-2026?redirect=https://attacker.example`
- **THEN** the redirect parameter is ignored or rejected and the system uses only the stored event URL for the resolved event

---

### Requirement: Registration-specific email links use the public event slug
The system SHALL generate attendee-facing registration links in emails from the configured public tickets base URL and `TicketedEvent.PublicSlug`, rather than using arbitrary event-owned URLs as the visible primary CTA domain. Registration-specific links SHALL include the event slug and the registration identifier needed by the existing public flow.

#### Scenario: Ticket email uses Admitto-owned QR link
- **WHEN** a ticket email is prepared for registration `R1` on event slug `azure-fest-2026`
- **THEN** the QR-code link starts with `https://tickets.admitto.org/e/azure-fest-2026/`

#### Scenario: Ticket email uses Admitto-owned cancel link
- **WHEN** a ticket email is prepared for registration `R1` on event slug `azure-fest-2026`
- **THEN** the cancellation link starts with `https://tickets.admitto.org/e/azure-fest-2026/`
