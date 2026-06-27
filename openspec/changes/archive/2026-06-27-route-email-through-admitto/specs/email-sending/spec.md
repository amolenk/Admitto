## MODIFIED Requirements

### Requirement: Email module sends a registration-confirmation email when an attendee is registered
The Email module SHALL send one registration-confirmation ("ticket") email per successful attendee registration when a worker can acquire the e-mail send claim for that registration occurrence. The email SHALL be triggered by the `AttendeeRegisteredIntegrationEvent` published by the Registrations module. The trigger handler SHALL prepare durable e-mail work by writing an `EmailLog` claim and enqueueing an internal delivery command in the Email outbox; actual SMTP delivery SHALL happen only from the delivery command after that claim has been committed by the message dispatcher. The email SHALL be sent via the Admitto system SMTP sender configured for the deployment. Email composition SHALL use the resolved built-in template for type `ticket` and SHALL include team branding values and event public-link values from module-owned context. Sending SHALL happen out-of-band from the originating registration request; the registration MUST succeed even if the email cannot be sent. A successfully committed outbox message that cannot be flushed to the queue immediately SHALL remain pending and be dispatched later by Worker-owned outbox retry processing.

#### Scenario: Successful send for a self-service registration
- **WHEN** an attendee "alice@example.com" successfully self-registers for event "DevConf" and the deployment has valid system SMTP configuration
- **THEN** within the worker's processing window, one email of type `ticket` is sent to "alice@example.com" via the configured Admitto SMTP sender, addressed from the configured Admitto from-address, and the email log records the send as `Sent`

#### Scenario: Send is not coupled to the registration request
- **WHEN** an attendee successfully registers but the SMTP server is temporarily unreachable
- **THEN** the registration response still indicates success, the integration event is enqueued or remains in a pending outbox row, and the email remains retryable until it succeeds or reaches the configured terminal failure policy

#### Scenario: Immediate outbox flush fails after registration commit
- **WHEN** registration commits an `AttendeeRegisteredIntegrationEvent` to the Registrations outbox but the immediate queue send fails
- **THEN** the outbox row remains `Pending` and Worker-owned outbox retry processing later sends it to the queue without requiring another registration request

#### Scenario: Delivery command is committed before SMTP
- **WHEN** the Email module handles an `AttendeeRegisteredIntegrationEvent`
- **THEN** it commits the `EmailLog` claim and internal delivery command before any SMTP send is attempted

#### Scenario: Missing system SMTP configuration no error to attendee
- **WHEN** an attendee successfully registers but the deployment's system SMTP configuration is missing or invalid
- **THEN** the registration succeeds, no SMTP send is attempted successfully, and the email log or operational telemetry records the configuration failure

---

## ADDED Requirements

### Requirement: Application email uses Admitto system sender identity
All application email sent by the Email module SHALL use a configured Admitto-controlled sender address for the SMTP `From` address. The visible display name MAY include the event name or another Admitto-controlled display value, but the sender email-address domain SHALL remain Admitto-controlled.

#### Scenario: Event display name with Admitto from-address
- **WHEN** the system sends a ticket email for event "Azure Fest 2026"
- **THEN** the message uses an Admitto-controlled `From` address and may use "Azure Fest 2026" as the display name

### Requirement: Built-in email templates use team accent color
Built-in email templates SHALL receive the owning team's accent color as a rendering parameter. When the team has no explicit accent color, the system default accent color SHALL be used.

#### Scenario: Team accent color is rendered in ticket email
- **WHEN** team "acme" has accent color `#0f766e` and a ticket email is rendered for one of its events
- **THEN** the rendered email uses `#0f766e` for accent-colored template elements

### Requirement: Ticket email includes change-tickets link only for multiple public ticket types
The ticket-confirmation email SHALL include a change-tickets link only when the event has at least two ticket types with `SelfServiceEnabled == true`. Sold-out state and waitlist mode SHALL NOT suppress the link. When fewer than two public self-service ticket types exist, the template SHALL omit the change-tickets section entirely.

#### Scenario: Change-tickets link included for two public ticket types
- **WHEN** a ticket email is prepared for an event with two self-service-enabled ticket types
- **THEN** the email parameters include a change-tickets link and the rendered ticket email shows the change-tickets CTA

#### Scenario: Change-tickets link omitted for one public ticket type
- **WHEN** a ticket email is prepared for an event with one self-service-enabled ticket type
- **THEN** the email parameters do not include a change-tickets link and the rendered ticket email omits the change-tickets CTA

#### Scenario: Sold-out public ticket still counts
- **WHEN** a ticket email is prepared for an event with two self-service-enabled ticket types and one is sold out
- **THEN** the email parameters include a change-tickets link

#### Scenario: Waitlist-mode public ticket still counts
- **WHEN** a ticket email is prepared for an event with two self-service-enabled ticket types and one is in waitlist mode
- **THEN** the email parameters include a change-tickets link
