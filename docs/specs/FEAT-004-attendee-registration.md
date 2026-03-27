# Feature Specification: Attendee Registration

## 1. Overview

| Field           | Value                                                           |
| --------------- | --------------------------------------------------------------- |
| Feature ID      | FEAT-004                                                        |
| Status          | Draft                                                           |
| Author          | Copilot + User                                                  |
| Created         | 2026-03-27                                                      |
| Last updated    | 2026-03-27                                                      |
| Epic / Parent   | Registrations Module                                            |
| Arc42 reference | 5. Building Block View — Registrations module                   |

### 1.1 Problem Statement

Event organizers need multiple pathways for attendee registration:

1. **Admin registration** — organizers manually register attendees (VIP guests,
   speakers) without any restrictions (capacity, window, domain).
2. **Self-service registration** — attendees register themselves, subject to capacity
   limits, registration windows, and optionally restricted to a specific email domain.
3. **Invite-based registration** — attendees use a single-use coupon to register for
   allowlisted ticket types, bypassing capacity and domain restrictions. Coupons
   optionally bypass the registration window too.

The Registrations module owns capacity tracking (via a separate `EventCapacity`
aggregate), registration window configuration, and email domain restrictions — keeping
the Organization module free of registration concerns.

### 1.2 Goal

Enable admin, self-service, and invite-based attendee registration with:

- Ticket type selection with domain validation (no duplicates, no overlapping time
  slots, valid types)
- Per-ticket-type capacity enforcement (with coupon and admin bypass)
- Registration window enforcement (open/close datetime; admin and optionally coupon
  bypass)
- Optional per-event email domain restriction (admin and coupon bypass)
- Coupon redemption: single-use, restricts available ticket types, capacity + domain
  bypass, optional window bypass

### 1.3 Non-Goals

- **Coupon management** (create, list, revoke) — separate spec
- Registration cancellation or modification after creation
- Reconfirmation / check-in / no-show tracking
- Email delivery mechanics (separate email module)
- Payment processing (Admitto targets free events)
- Waitlists
- Coupons as discount codes

## 2. User Stories

### US-001: Admin registration

**As an** organizer,
**I want** to register an attendee on their behalf,
**so that** I can accommodate VIP guests and speakers without restrictions.

### US-002: Self-service registration

**As an** attendee,
**I want** to register myself for an event,
**so that** I can secure a spot.

### US-003: Invite-based registration

**As an** attendee with an invite,
**I want** to register using my coupon code,
**so that** I can attend with my reserved access — even if the event is full.

### US-004: Registration window configuration

**As an** organizer,
**I want** to set a registration window (open/close dates) for my event,
**so that** registrations are only accepted during a specific time period.

### US-005: Email domain restriction

**As an** organizer,
**I want** to restrict self-service registration to a specific email domain,
**so that** only authorized people can sign up.

## 3. Functional Requirements

### Admin Registration (US-001)

| ID     | Requirement                                                                                                                                         | Priority | User Story |
| ------ | --------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall allow organizers (Owner or Organizer role) to register an attendee by providing their email, attendee info, and selected ticket types. | Must     | US-001     |
| FR-002 | Admin registrations shall bypass capacity enforcement, registration window, and email domain restrictions.                                            | Must     | US-001     |

### Self-Service Registration (US-002)

| ID     | Requirement                                                                                                                                                           | Priority | User Story |
| ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-003 | The system shall allow attendees to register themselves by providing their email, attendee info, and selected ticket types via a public endpoint.                       | Must     | US-002     |
| FR-004 | The system shall enforce per-ticket-type capacity for self-service registrations and reject the registration if any selected ticket type is at capacity.                | Must     | US-002     |
| FR-005 | The system shall require a registration window to be configured for self-service registration. If configured, the system shall reject self-service registrations outside the window. | Must     | US-002     |
| FR-006 | If an event has an email domain restriction configured, the system shall reject self-service registrations from email addresses that do not match the allowed domain.    | Must     | US-002     |

### Invite-Based Registration (US-003)

| ID     | Requirement                                                                                                                     | Priority | User Story |
| ------ | ------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-007 | The system shall allow attendees to register using a valid, unexpired, single-use coupon code.                                   | Must     | US-003     |
| FR-008 | Coupon-based registrations shall restrict ticket type selection to the coupon's allowlisted types.                                | Must     | US-003     |
| FR-009 | Coupon-based registrations shall bypass capacity enforcement and email domain restrictions.                                       | Must     | US-003     |
| FR-010 | If the coupon has `bypassRegistrationWindow` set, the registration shall also bypass the registration window check.               | Must     | US-003     |
| FR-011 | Upon successful registration, the system shall mark the coupon as redeemed (single-use).                                         | Must     | US-003     |

### Common Validation (US-001, US-002, US-003)

| ID     | Requirement                                                                                                                  | Priority | User Story         |
| ------ | ---------------------------------------------------------------------------------------------------------------------------- | -------- | ------------------ |
| FR-012 | The system shall reject registrations with duplicate ticket types in the selection.                                            | Must     | US-001, 002, 003   |
| FR-013 | The system shall reject registrations referencing non-existent or cancelled ticket types.                                      | Must     | US-001, 002, 003   |
| FR-014 | The system shall reject registrations where selected ticket types have overlapping time slots.                                 | Must     | US-001, 002, 003   |
| FR-015 | The system shall reject all registrations (admin, self-service, and coupon) for cancelled or archived events.                  | Must     | US-001, 002, 003   |
| FR-016 | The system shall reject registrations if the email address is already registered for the same event.                           | Must     | US-001, 002, 003   |

### Registration Configuration (US-004, US-005)

| ID     | Requirement                                                                                                                       | Priority | User Story |
| ------ | --------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-017 | The system shall allow organizers to configure a registration window (open and close datetimes) for an event.                       | Must     | US-004     |
| FR-018 | The system shall allow organizers to configure an optional email domain restriction for an event (single domain pattern).           | Must     | US-005     |

### Capacity Synchronization (internal)

| ID     | Requirement                                                                                                                                                           | Priority | User Story |
| ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-019 | The system shall automatically initialize and update per-ticket-type capacity in the Registrations module when ticket types are created or modified in the Organization module (via module events). | Must     | —          |

## 4. Acceptance Scenarios

### Admin Registration

#### SC-001: Successful admin registration (FR-001)

```gherkin
Given an active event "DevConf" with ticket type "General Admission" (capacity: 100, 0 used)
  And the registration window is currently open
When an organizer registers attendee "alice@example.com" for "General Admission"
Then a registration is created for "alice@example.com" with ticket "General Admission"
  And the capacity used for "General Admission" remains 0
```

#### SC-002: Admin registration bypasses capacity (FR-002)

```gherkin
Given an active event "DevConf" with ticket type "Workshop" (capacity: 20, 20 used)
When an organizer registers attendee "bob@example.com" for "Workshop"
Then a registration is created for "bob@example.com" with ticket "Workshop"
  And the capacity used for "Workshop" remains 20
```

#### SC-003: Admin registration bypasses closed registration window (FR-002)

```gherkin
Given an active event "DevConf" with a registration window that has closed
When an organizer registers attendee "carol@example.com" for "General Admission"
Then a registration is created for "carol@example.com"
```

#### SC-004: Admin registration bypasses domain restriction (FR-002)

```gherkin
Given an active event "CorpConf" restricted to domain "@acme.com"
When an organizer registers attendee "speaker@gmail.com" for "General Admission"
Then a registration is created for "speaker@gmail.com"
```

### Self-Service Registration

#### SC-005: Successful self-service registration (FR-003, FR-004)

```gherkin
Given an active event "DevConf" with ticket type "General Admission" (capacity: 100, 50 used)
  And the registration window is currently open
  And no domain restriction is configured
When an attendee self-registers as "dave@example.com" for "General Admission"
Then a registration is created for "dave@example.com" with ticket "General Admission"
  And the capacity used for "General Admission" increases to 51
```

#### SC-006: Self-service rejected — capacity full (FR-004)

```gherkin
Given an active event "DevConf" with ticket type "Workshop" (capacity: 20, 20 used)
  And the registration window is currently open
When an attendee self-registers as "eve@example.com" for "Workshop"
Then the registration is rejected with reason "ticket type at capacity"
```

#### SC-007: Self-service rejected — before registration window opens (FR-005)

```gherkin
Given an active event "DevConf" with a registration window opening tomorrow
When an attendee self-registers as "frank@example.com" for "General Admission"
Then the registration is rejected with reason "registration not open"
```

#### SC-008: Self-service rejected — after registration window closes (FR-005)

```gherkin
Given an active event "DevConf" with a registration window that closed yesterday
When an attendee self-registers as "grace@example.com" for "General Admission"
Then the registration is rejected with reason "registration closed"
```

#### SC-009: Self-service rejected — email domain mismatch (FR-006)

```gherkin
Given an active event "CorpConf" restricted to domain "@acme.com"
  And the registration window is currently open
When an attendee self-registers as "outsider@gmail.com" for "General Admission"
Then the registration is rejected with reason "email domain not allowed"
```

#### SC-010: Self-service allowed — email domain matches (FR-006)

```gherkin
Given an active event "CorpConf" restricted to domain "@acme.com"
  And the registration window is currently open
When an attendee self-registers as "employee@acme.com" for "General Admission"
Then a registration is created for "employee@acme.com"
```

#### SC-011: Self-service rejected — no registration window configured (FR-005)

```gherkin
Given an active event "DevConf" with no registration window configured
When an attendee self-registers as "heidi@example.com" for "General Admission"
Then the registration is rejected with reason "registration not open"
```

### Invite-Based Registration

#### SC-012: Successful coupon registration (FR-007, FR-009, FR-011)

```gherkin
Given an active event "DevConf" with ticket type "Speaker Pass" (capacity: 5, 5 used)
  And a valid coupon "INVITE-001" allowlisting "Speaker Pass" that has not expired
  And the registration window is currently open
When an attendee registers as "speaker@gmail.com" using coupon "INVITE-001" for "Speaker Pass"
Then a registration is created for "speaker@gmail.com" with ticket "Speaker Pass"
  And coupon "INVITE-001" is marked as redeemed
  And the capacity used for "Speaker Pass" remains 5
```

#### SC-013: Coupon rejected — expired (FR-007)

```gherkin
Given a coupon "INVITE-002" that expired yesterday
When an attendee registers using coupon "INVITE-002"
Then the registration is rejected with reason "coupon expired"
```

#### SC-014: Coupon rejected — already redeemed (FR-007)

```gherkin
Given a coupon "INVITE-003" that has already been redeemed
When an attendee registers using coupon "INVITE-003"
Then the registration is rejected with reason "coupon already used"
```

#### SC-015: Coupon rejected — ticket type not allowlisted (FR-008)

```gherkin
Given a valid coupon "INVITE-004" allowlisting only "Speaker Pass"
When an attendee registers using coupon "INVITE-004" for "General Admission"
Then the registration is rejected with reason "ticket type not allowed for this coupon"
```

#### SC-016: Coupon bypasses registration window (flag set) (FR-010)

```gherkin
Given an active event with a registration window that has closed
  And a valid coupon "INVITE-005" with bypassRegistrationWindow enabled
When an attendee registers using coupon "INVITE-005" for "Speaker Pass"
Then a registration is created
```

#### SC-017: Coupon respects registration window (flag not set) (FR-010)

```gherkin
Given an active event with a registration window that has closed
  And a valid coupon "INVITE-006" with bypassRegistrationWindow disabled
When an attendee registers using coupon "INVITE-006" for "Speaker Pass"
Then the registration is rejected with reason "registration closed"
```

#### SC-018: Coupon bypasses domain restriction (FR-009)

```gherkin
Given an active event restricted to domain "@acme.com"
  And a valid coupon "INVITE-007" allowlisting "General Admission"
  And the registration window is currently open
When an attendee registers as "external@gmail.com" using coupon "INVITE-007" for "General Admission"
Then a registration is created for "external@gmail.com"
```

### Common Validation

#### SC-019: Rejected — duplicate ticket types in selection (FR-012)

```gherkin
Given an active event with ticket type "General Admission"
  And the registration window is currently open
When an attendee registers selecting "General Admission" twice
Then the registration is rejected with reason "duplicate ticket types"
```

#### SC-020: Rejected — non-existent ticket type (FR-013)

```gherkin
Given an active event
  And the registration window is currently open
When an attendee registers selecting ticket type "Premium VIP" which does not exist
Then the registration is rejected with reason "unknown ticket type"
```

#### SC-021: Rejected — cancelled ticket type (FR-013)

```gherkin
Given ticket type "Workshop A" has been cancelled
  And the registration window is currently open
When an attendee registers selecting "Workshop A"
Then the registration is rejected with reason "ticket type cancelled"
```

#### SC-022: Rejected — overlapping time slots (FR-014)

```gherkin
Given ticket type "Workshop A" runs 09:00–11:00 and "Workshop B" runs 10:00–12:00
  And the registration window is currently open
When an attendee registers selecting both "Workshop A" and "Workshop B"
Then the registration is rejected with reason "overlapping time slots"
```

#### SC-023: Rejected — cancelled event (FR-015)

```gherkin
Given event "OldConf" has been cancelled
When an attendee attempts to register for "OldConf"
Then the registration is rejected with reason "event not active"
```

#### SC-024: Rejected — duplicate email (FR-016)

```gherkin
Given "alice@example.com" is already registered for event "DevConf"
  And the registration window is currently open
When "alice@example.com" attempts to register again for "DevConf"
Then the registration is rejected with reason "already registered"
```

### Registration Configuration

#### SC-025: Configure registration window (FR-017)

```gherkin
Given an active event "DevConf"
When an organizer sets the registration window from "2025-01-01T00:00Z" to "2025-06-01T00:00Z"
Then the registration window is saved for "DevConf"
```

#### SC-026: Configure email domain restriction (FR-018)

```gherkin
Given an active event "CorpConf"
When an organizer sets the allowed email domain to "@acme.com"
Then self-service registrations for "CorpConf" are restricted to "@acme.com" emails
```

#### SC-027: Remove email domain restriction (FR-018)

```gherkin
Given an active event "CorpConf" with domain restriction "@acme.com"
When an organizer removes the email domain restriction
Then self-service registrations for "CorpConf" accept any email domain
```

### Capacity Synchronization

#### SC-028: Capacity initialized when ticket type is created (FR-019)

```gherkin
Given an active event "DevConf" in the Organization module
When a ticket type "General Admission" with capacity 100 is added to "DevConf"
Then the Registrations module initializes capacity tracking for "General Admission"
  with max capacity 100 and 0 used
```

#### SC-029: Capacity updated when ticket type capacity changes (FR-019)

```gherkin
Given capacity tracking exists for "General Admission" with max capacity 100 and 50 used
When the Organization module updates "General Admission" capacity to 150
Then the Registrations module updates max capacity for "General Admission" to 150
  with used count unchanged
```

## 5. Domain Model

### 5.1 Entities

#### Registration (aggregate root — existing, extended)

Represents a single attendee's registration for an event.

| Attribute      | Type              | Constraints                         | Description                           |
| -------------- | ----------------- | ----------------------------------- | ------------------------------------- |
| id             | UUID              | PK, generated                       |                                       |
| eventId        | UUID              | Required, cross-module reference    | References TicketedEvent in Org module |
| email          | string            | Required, normalized to lowercase   |                                       |
| attendeeInfo   | AttendeeInfo      | Required                            | Attendee details (name, etc.)         |
| tickets        | List\<Ticket\>    | At least one                        | Selected ticket types                 |
| ticketGrantMode| TicketGrantMode   | Required                            | SelfService or Privileged             |
| couponId       | UUID?             | Nullable                            | Set when coupon used; **new**         |
| createdAt      | datetime          | Generated, immutable                |                                       |

#### EventCapacity (aggregate root — existing)

Tracks per-ticket-type capacity within the Registrations module, separate from
the Organization module's event data.

| Attribute         | Type                     | Constraints    | Description                    |
| ----------------- | ------------------------ | -------------- | ------------------------------ |
| id                | UUID                     | PK, generated  |                                |
| eventId           | UUID                     | Unique         | One capacity record per event  |
| ticketCapacities  | List\<TicketCapacity\>   | One per type   |                                |

#### TicketCapacity (entity within EventCapacity — existing)

| Attribute      | Type     | Constraints                     | Description         |
| -------------- | -------- | ------------------------------- | ------------------- |
| ticketTypeId   | UUID     | Unique within aggregate         |                     |
| maxCapacity    | integer  | ≥ 0                             | Synced from Org     |
| usedCapacity   | integer  | ≥ 0, ≤ maxCapacity              | Incremented on use  |

#### RegistrationSettings (aggregate root — **new**)

Per-event registration configuration, owned by the Registrations module.

| Attribute                 | Type      | Constraints                           | Description                                |
| ------------------------- | --------- | ------------------------------------- | ------------------------------------------ |
| id                        | UUID      | PK, generated                         |                                            |
| eventId                   | UUID      | Unique                                | One settings record per event              |
| registrationWindowOpen    | datetime? | Nullable                              | When self-service opens                    |
| registrationWindowClose   | datetime? | Nullable                              | When self-service closes                   |
| allowedEmailDomain        | string?   | Nullable, e.g., "@acme.com"           | If set, self-service restricted to domain  |

### 5.2 Relationships

- A **Registration** references an event in the Organization module (via eventId,
  cross-module).
- A **Registration** optionally references a **Coupon** (via couponId, within the
  Registrations module).
- An **EventCapacity** aggregate tracks capacity for one event and contains multiple
  **TicketCapacity** entries (one per ticket type).
- A **RegistrationSettings** record exists per event (one-to-one with event).

### 5.3 Value Objects

#### AttendeeInfo (existing)

Attendee details (name, etc.) — structure determined by implementation.

#### Ticket (existing)

| Attribute    | Type | Constraints |
| ------------ | ---- | ----------- |
| ticketTypeId | UUID | Required    |

#### TicketGrantMode (existing)

Enum: `SelfService`, `Privileged`

#### CapacityEnforcementMode (existing)

Enum: `Enforce`, `Ignore`

#### Coupon (referenced — managed by separate spec)

The registration flow depends on coupon data. The coupon entity must expose:

| Attribute                  | Type          | Purpose                       |
| -------------------------- | ------------- | ----------------------------- |
| id                         | UUID          | Identity                      |
| eventId                    | UUID          | Scoped to event               |
| code                       | string        | Unique lookup key             |
| allowedTicketTypeIds       | List\<UUID\>  | Ticket type allowlist         |
| expiresAt                  | datetime      | Expiry check                  |
| bypassRegistrationWindow   | boolean       | Window bypass flag            |
| redeemedAt                 | datetime?     | Null = not yet used           |

### 5.4 Domain Rules and Invariants

1. **At least one ticket**: A registration must contain at least one ticket.
2. **No duplicate tickets**: Ticket types within a registration must not contain
   duplicates.
3. **No overlapping time slots**: Selected ticket types must not have overlapping
   time slots.
4. **Valid ticket types**: All selected ticket types must exist and not be cancelled.
5. **Capacity enforcement**: Self-service registrations must respect per-ticket-type
   capacity (enforced via `EventCapacity.Claim()`).
6. **Registration window required**: Self-service registrations require a configured
   registration window. If no window is set, self-service registration is rejected.
   If set, the registration must fall within it.
7. **Email domain enforcement**: Self-service registrations must match the allowed
   email domain (if configured). Domain match is exact suffix.
8. **Coupon allowlist**: Coupon-based registrations may only select from the coupon's
   allowlisted ticket types.
9. **Single-use coupon**: A coupon can be redeemed at most once.
10. **Unique email per event**: No two registrations for the same event may share the
    same email address.
11. **Active events only**: Registrations cannot be created for cancelled or archived
    events.
12. **Capacity sync**: When a ticket type's capacity changes in the Organization
    module, the Registrations module's `EventCapacity` must reflect the new maximum.

## 6. Non-Functional Requirements

| ID      | Category      | Requirement                                                                                                                                              |
| ------- | ------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Concurrency   | Concurrent self-service registrations for the same ticket type must be handled safely via optimistic concurrency on EventCapacity. At most one registration succeeds for the last available spot. |
| NFR-002 | Atomicity     | Registration creation, capacity claim, and coupon redemption must be atomic — if any step fails, nothing is committed.                                   |
| NFR-003 | Performance   | Self-service registration should complete in < 500ms at p95 under normal load. The CapacityTracker pattern (defer capacity check after domain validation) minimizes the contention window on EventCapacity. |
| NFR-004 | Authorization | Admin registration and configuration endpoints require Owner or Organizer role. Self-service and coupon-based endpoints are public (unauthenticated).     |
| NFR-005 | Module boundary | Ticket type data is read via `IOrganizationFacade`. Capacity sync uses module events from Organization → Registrations. No direct cross-module DbContext access. |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                              | Expected Behavior                                                                                               |
| ----- | ----------------------------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| EC-01 | Two attendees claim the last ticket simultaneously    | One registration succeeds; the other is rejected with "ticket type at capacity". Optimistic concurrency on EventCapacity ensures correctness. |
| EC-02 | Same coupon code submitted twice concurrently         | Only one registration succeeds; the other is rejected with "coupon already used".                               |
| EC-03 | Ticket type cancelled after coupon was created        | Registration rejected — cancelled ticket types are rejected regardless of coupon.                               |
| EC-04 | Event cancelled between coupon issue and use          | Registration rejected — "event not active".                                                                     |
| EC-05 | Capacity sync arrives during concurrent registration  | Optimistic concurrency on EventCapacity handles this — the registration retries or fails gracefully.            |
| EC-06 | Email case sensitivity                                | Emails are normalized to lowercase for uniqueness checks. "Alice@Example.com" and "alice@example.com" are the same registrant. |
| EC-07 | Registration at window open/close boundary            | Window is inclusive of `open` and exclusive of `close` (>= open, < close).                                      |
| EC-08 | Capacity reduced below current usage via sync         | Max capacity is updated; existing registrations are not revoked. New registrations are rejected if used >= new max. |
| EC-09 | Self-service for event with no ticket types yet       | Registration rejected — at least one valid ticket type must be selected, and none exist.                        |
| EC-10 | Domain restriction: subdomain handling                | Domain match is exact suffix. "@acme.com" matches "user@acme.com" but NOT "user@sub.acme.com".                 |
| EC-11 | Coupon used for different event than coupon's eventId | Registration rejected — coupon is scoped to a specific event.                                                   |

## 8. Success Criteria

| ID   | Criterion                                                                              |
| ---- | -------------------------------------------------------------------------------------- |
| S-01 | All 29 acceptance scenarios pass in CI.                                                |
| S-02 | Admin registration works end-to-end with capacity bypass verified.                     |
| S-03 | Self-service registration enforces capacity, registration window, and domain correctly. |
| S-04 | Coupon-based registration correctly bypasses capacity and domain, conditionally bypasses window. |
| S-05 | Concurrent last-spot registrations result in exactly one success (no overbooking).     |
| S-06 | Capacity sync from Organization module reflected in Registrations within event delivery guarantees. |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **FEAT-003 (Event Management)**: Events and ticket types must exist before
  registrations can be created. Ticket type capacity drives the capacity sync.
- **Coupon Management (future spec)**: Coupon CRUD is specced separately. This
  feature depends on the coupon entity shape defined in section 5.3.
- **Organization Facade**: `IOrganizationFacade` must expose ticket type data
  (including time slots and cancellation status) to the Registrations module.
- **Module Event Infrastructure**: Capacity sync relies on module events from
  Organization → Registrations when ticket types are created or updated.
- **Email Module (future)**: Email verification of attendees is handled by a
  separate email module and is not part of this spec.

### 9.2 Constraints

- The Registrations module must not access the Organization module's DbContext
  directly (ADR-001: modular monolith boundaries).
- Capacity tracking is owned by the Registrations module's `EventCapacity`
  aggregate, not the Organization module's `TicketedEvent`.
- All cross-module data flows use the facade pattern or module events.

### 9.3 Architecture References

| Arc42 Section                    | Relevance to This Feature                                                     |
| -------------------------------- | ----------------------------------------------------------------------------- |
| 5. Building Block View           | Registrations module structure, Registration + EventCapacity aggregates       |
| 6. Runtime View                  | Registration flow, capacity claiming, cross-module facade calls               |
| 8. Crosscutting Concepts         | Module event taxonomy, outbox pattern, optimistic concurrency, facade pattern |
| 9. Architecture Decisions (ADRs) | ADR-001 (modular monolith), ADR-002 (feature-sliced endpoints)               |
| 10. Quality Requirements         | Performance targets, concurrency handling                                     |

## 10. Open Questions

_No open questions._

