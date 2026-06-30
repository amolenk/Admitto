## MODIFIED Requirements

### Requirement: Application email uses Admitto system sender identity

All application email sent by the Email module SHALL use a configured Admitto-controlled sender address for the SMTP `From` address. When the owning team has an optional reply-to email address, the Email module SHALL use that reply-to address as the visible MIME `From` display name and SHALL set the SMTP `Reply-To` header to that address without changing the `From` address. When the owning team has no reply-to email address, the visible MIME `From` display name SHALL remain the configured Admitto-controlled sender address.

#### Scenario: Team reply-to labels Admitto from-address

- **WHEN** the system sends a ticket email for a team with reply-to email address `help@example.com`
- **THEN** the message uses the Admitto-controlled `From` address, uses `help@example.com` as the visible `From` display name, and sets `Reply-To: help@example.com`

#### Scenario: Missing team reply-to keeps system sender label

- **WHEN** the system sends a ticket email for a team without a reply-to email address
- **THEN** the message uses the Admitto-controlled `From` address and uses the configured Admitto-controlled sender address as the visible `From` display name

---

### Requirement: Email owns team and event rendering context

The Email module SHALL persist Email-owned rendering context projections for each team and ticketed event that can receive application email. The projections SHALL contain only email-rendering, reply routing, sender-label, and scheduling facts, including the owning team id, ticketed event id, team accent color, optional team reply-to email address, event name, event website URL, public event slug or equivalent link inputs, event time zone, reconfirm policy snapshot when present, self-service ticket-type count, and lifecycle state needed by Email.

The projection SHALL be updated from Organization and Registrations integration events. Email SHALL NOT call Organization synchronously for team branding while preparing application email.

#### Scenario: Team accent color comes from Email projection

- **WHEN** a ticket email is prepared for an event whose Email projection has team accent color `#0f766e`
- **THEN** the email render parameters use `#0f766e` without calling the Organization facade for branding

#### Scenario: Team reply-to comes from Email projection

- **WHEN** a ticket email is sent for a team whose Email projection has reply-to email address `help@example.com`
- **THEN** the SMTP message uses `Reply-To: help@example.com` and visible `From` display name `help@example.com` without calling the Organization facade for team contact metadata

#### Scenario: Event links come from Email projection inputs

- **WHEN** a ticket email is prepared for an event whose Email projection has public slug `devconf-2026`
- **THEN** the ticket, cancel, QR-code, and change-ticket links are derived from the Email projection inputs and configured public base URL

#### Scenario: Missing required rendering context is recorded deterministically

- **WHEN** an email-triggering integration event is handled before the required event rendering context exists in Email
- **THEN** Email records or defers the send according to the send pipeline's deterministic failure/retry rules and does not query sibling module DbContexts directly
