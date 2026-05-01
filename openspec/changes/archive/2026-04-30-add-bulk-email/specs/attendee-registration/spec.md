## ADDED Requirements

### Requirement: Registration carries attendee identity, lifecycle status, and reconfirm state

The `Registration` aggregate (Registrations module) SHALL carry:

- Required `FirstName` and `LastName` value objects (string, 1–100 chars, trimmed). They are part of the aggregate's identity-facing data, not derived from the email.
- A `Status` enum with values `Registered` (initial state) and `Cancelled`.
- A `HasReconfirmed` boolean (default `false`) and a `ReconfirmedAt?` timestamp (set when reconfirm succeeds).

The aggregate SHALL expose two domain operations:

- `Cancel(reason)` — transitions `Status` from `Registered` to `Cancelled`, captures the supplied reason on the aggregate, and raises `RegistrationCancelledDomainEvent` (mapped via the module's message policy to `RegistrationCancelledIntegrationEvent` in `Admitto.Module.Registrations.Contracts`). Calling `Cancel` on a registration that is already `Cancelled` SHALL fail with a business-rule violation.
- `Reconfirm()` — sets `HasReconfirmed=true` and `ReconfirmedAt` to the current instant, and raises `RegistrationReconfirmedDomainEvent` (mapped to `RegistrationReconfirmedIntegrationEvent`). Calling `Reconfirm` on a `Cancelled` registration SHALL fail with a business-rule violation.

#### Scenario: New registration starts Registered, not reconfirmed
- **WHEN** a registration is created via any channel
- **THEN** `Status=Registered`, `HasReconfirmed=false`, `ReconfirmedAt=null`, and `FirstName`/`LastName` carry the supplied values

#### Scenario: Cancel transitions to Cancelled and emits an integration event
- **WHEN** an admin cancels a `Registered` registration with reason "duplicate signup"
- **THEN** `Status=Cancelled`, the reason is captured, and `RegistrationCancelledIntegrationEvent` is enqueued in the outbox

#### Scenario: Cancel rejected on already-cancelled registration
- **WHEN** Cancel is invoked on a registration whose `Status` is already `Cancelled`
- **THEN** the operation fails with a business-rule violation and no event is raised

#### Scenario: Reconfirm sets the flag and emits an integration event
- **WHEN** an attendee reconfirms an active registration
- **THEN** `HasReconfirmed=true`, `ReconfirmedAt` is the current instant, and `RegistrationReconfirmedIntegrationEvent` is enqueued

#### Scenario: Reconfirm rejected on cancelled registration
- **WHEN** Reconfirm is invoked on a registration whose `Status` is `Cancelled`
- **THEN** the operation fails with a business-rule violation

---

### Requirement: Self-registration and admin-add registration require first and last name

Every registration-creation channel — public self-service, coupon redemption, and admin-add — SHALL require non-empty `firstName` and `lastName` in the request DTO. FluentValidation SHALL reject requests that omit either field with a `400` response. The values SHALL be parsed via the `FirstName`/`LastName` value object factories so trim/length rules apply at the boundary.

#### Scenario: Self-service registration requires names
- **WHEN** an attendee posts a self-service registration without `firstName`
- **THEN** the API returns `400` with a validation error on `firstName`

#### Scenario: Admin-add registration requires names
- **WHEN** an admin posts an admin-add registration without `lastName`
- **THEN** the API returns `400` with a validation error on `lastName`

#### Scenario: Coupon-based registration requires names
- **WHEN** an attendee redeems a coupon without supplying `firstName` and `lastName`
- **THEN** the API returns `400` with validation errors on the missing fields

---

### Requirement: AttendeeRegisteredIntegrationEvent carries first and last name

The `AttendeeRegisteredIntegrationEvent` published from the Registrations module SHALL carry the registrant's `FirstName` and `LastName` (in addition to the existing `TeamId`, `TicketedEventId`, `RegistrationId`, and email). Downstream consumers (notably the Email module's single-send template renderer) SHALL use these fields rather than deriving a display name from the email.

#### Scenario: Names propagate to the integration event
- **WHEN** "Alice Anderson" registers as `alice@example.com`
- **THEN** the enqueued `AttendeeRegisteredIntegrationEvent` carries `FirstName="Alice"`, `LastName="Anderson"`, `Email="alice@example.com"`
