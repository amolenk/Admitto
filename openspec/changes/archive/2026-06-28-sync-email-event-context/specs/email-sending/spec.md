## ADDED Requirements

### Requirement: Email owns team and event rendering context

The Email module SHALL persist an Email-owned rendering context projection for each ticketed event that can receive application email. The projection SHALL contain only email-rendering and scheduling facts, including the owning team id, ticketed event id, team accent color, event name, event website URL, public event slug or equivalent link inputs, event time zone, reconfirm policy snapshot when present, self-service ticket-type count, and lifecycle state needed by Email.

The projection SHALL be updated from Organization and Registrations integration events. Email SHALL NOT call Organization synchronously for team branding while preparing application email.

#### Scenario: Team accent color comes from Email projection

- **WHEN** a ticket email is prepared for an event whose Email projection has team accent color `#0f766e`
- **THEN** the email render parameters use `#0f766e` without calling the Organization facade for branding

#### Scenario: Event links come from Email projection inputs

- **WHEN** a ticket email is prepared for an event whose Email projection has public slug `devconf-2026`
- **THEN** the ticket, cancel, QR-code, and change-ticket links are derived from the Email projection inputs and configured public base URL

#### Scenario: Missing required rendering context is recorded deterministically

- **WHEN** an email-triggering integration event is handled before the required event rendering context exists in Email
- **THEN** Email records or defers the send according to the send pipeline's deterministic failure/retry rules and does not query sibling module DbContexts directly

### Requirement: Transactional email handlers combine trigger payload facts with Email context

Transactional email handlers SHALL use the triggering integration-event payload for occurrence-specific facts such as recipient address, registration id, attendee name when supplied, ticket names, OTP code, coupon code, cancellation reason, and idempotency timestamp. They SHALL use the Email-owned event rendering context projection for reusable team/event facts such as event name, website URL, public links, team accent color, and change-ticket availability.

Transactional email handlers SHALL NOT use `IRegistrationsFacade.GetEventRegistrationSnapshotAsync` to obtain reusable team/event rendering context.

#### Scenario: Registration confirmation combines payload and projection

- **WHEN** Email handles an `AttendeeRegisteredIntegrationEvent`
- **THEN** recipient, attendee name, ticket names, registration id, and idempotency timestamp come from the event payload, while event name, website URL, public links, team accent color, and change-ticket availability come from the Email projection

#### Scenario: OTP email uses projected branding

- **WHEN** Email handles an OTP-code requested event for an event with projected team accent color
- **THEN** the verification-code email uses the projected accent color and the OTP code from the trigger payload

### Requirement: Email rendering context is eventually consistent

Application email rendering SHALL tolerate eventual consistency between Organization/Registrations changes and the Email-owned rendering context projection. A queued email MAY render using the latest context that has reached Email at handling time. Email SHALL NOT use the projection for registration correctness, authorization, ticket capacity, or attendee eligibility decisions.

#### Scenario: Recent branding change may lag

- **WHEN** a team accent color is changed and a ticket email is handled before the corresponding branding integration event updates Email
- **THEN** the email may render with the previous accent color and the send remains valid

#### Scenario: Projection not used for capacity decisions

- **WHEN** an attendee registers or changes tickets
- **THEN** ticket capacity and lifecycle checks are still enforced by Registrations-owned aggregates, not by Email rendering context
