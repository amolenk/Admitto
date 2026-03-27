# Feature Specification: Coupon Management

## 1. Overview

| Field           | Value                                                         |
| --------------- | ------------------------------------------------------------- |
| Feature ID      | FEAT-005                                                      |
| Status          | Draft                                                         |
| Author          | Copilot + User                                                |
| Created         | 2026-03-27                                                    |
| Last updated    | 2026-03-27                                                    |
| Epic / Parent   | Registrations Module                                          |
| Arc42 reference | 5. Building Block View — Registrations module                 |

### 1.1 Problem Statement

Organizers need a way to invite specific people to their events by creating
single-use coupon codes. A coupon grants access to selected ticket types and
bypasses capacity limits and email domain restrictions (as defined in FEAT-004).
Coupons have an expiry and optionally bypass the registration window. Today there
is no way to create, view, or revoke coupons — the entity shape is referenced by
FEAT-004 but no lifecycle management exists.

### 1.2 Goal

Enable organizers to create, list, view, and revoke coupons for their events. When
a coupon is created, the system triggers an invitation email and makes the coupon
code visible to the organizer. Coupons are persisted as entities in the
Registrations module.

### 1.3 Non-Goals

- Coupon redemption during registration (covered by FEAT-004)
- Batch/bulk coupon generation
- Coupons as discount codes (Admitto is free-events only)
- Coupon templates or reusable coupon definitions
- Email delivery mechanics (separate email module)
- Modification of coupon attributes after creation

## 2. User Stories

### US-001: Create coupon

**As an** organizer,
**I want** to create a coupon for a specific attendee,
**so that** I can invite them to my event with reserved ticket access.

### US-002: List coupons

**As an** organizer,
**I want** to list all coupons for an event,
**so that** I can see who has been invited and the status of each coupon.

### US-003: View coupon details

**As an** organizer,
**I want** to view a single coupon's details including its code,
**so that** I can share the invitation link manually if needed.

### US-004: Revoke coupon

**As an** organizer,
**I want** to revoke a coupon,
**so that** I can cancel an invitation before it's used.

## 3. Functional Requirements

### Coupon Creation (US-001)

| ID     | Requirement                                                                                                                                                                              | Priority | User Story |
| ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall allow organizers (Owner or Organizer role) to create a coupon by specifying: target email, allowlisted ticket type(s), expiry datetime, and whether the coupon bypasses the registration window. | Must     | US-001     |
| FR-002 | The system shall generate a unique coupon code (GUID-based) upon creation.                                                                                                                | Must     | US-001     |
| FR-003 | The system shall trigger an invitation email to the target email address upon coupon creation.                                                                                             | Must     | US-001     |
| FR-004 | The system shall reject coupon creation if any specified ticket type does not exist or is cancelled.                                                                                       | Must     | US-001     |
| FR-005 | The system shall reject coupon creation if the expiry datetime is in the past.                                                                                                            | Must     | US-001     |
| FR-006 | The system shall reject coupon creation for cancelled or archived events.                                                                                                                  | Must     | US-001     |

### Coupon Listing (US-002)

| ID     | Requirement                                                                                                                                                                  | Priority | User Story |
| ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-007 | The system shall allow organizers to list all coupons for an event, showing: target email, status (active/redeemed/revoked/expired), allowlisted ticket types, expiry, and creation date. | Must     | US-002     |

### Coupon Details (US-003)

| ID     | Requirement                                                                                                        | Priority | User Story |
| ------ | ------------------------------------------------------------------------------------------------------------------ | -------- | ---------- |
| FR-008 | The system shall allow organizers to view a single coupon's full details including the coupon code.                  | Must     | US-003     |

### Coupon Revocation (US-004)

| ID     | Requirement                                                                                                           | Priority | User Story |
| ------ | --------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-009 | The system shall allow organizers to revoke an active or expired coupon, preventing it from being used for registration. | Must     | US-004     |
| FR-010 | The system shall reject revocation of a coupon that has already been redeemed.                                          | Must     | US-004     |

## 4. Acceptance Scenarios

### Coupon Creation

#### SC-001: Successful coupon creation (FR-001, FR-002, FR-003)

```gherkin
Given an active event "DevConf" with ticket type "Speaker Pass"
When an organizer creates a coupon for "speaker@example.com"
  allowlisting "Speaker Pass" expiring "2025-06-01T00:00Z"
  with bypassRegistrationWindow disabled
Then a coupon is created with a unique code
  And an invitation email is triggered for "speaker@example.com"
```

#### SC-002: Coupon with registration window bypass (FR-001)

```gherkin
Given an active event "DevConf" with ticket type "VIP Pass"
When an organizer creates a coupon for "vip@example.com"
  allowlisting "VIP Pass" with bypassRegistrationWindow enabled
Then a coupon is created with bypassRegistrationWindow set to true
```

#### SC-003: Rejected — ticket type does not exist (FR-004)

```gherkin
Given an active event "DevConf"
When an organizer creates a coupon allowlisting "Premium VIP" which does not exist
Then the coupon creation is rejected with reason "unknown ticket type"
```

#### SC-004: Rejected — ticket type is cancelled (FR-004)

```gherkin
Given an active event "DevConf" with cancelled ticket type "Workshop A"
When an organizer creates a coupon allowlisting "Workshop A"
Then the coupon creation is rejected with reason "ticket type cancelled"
```

#### SC-005: Rejected — expiry in the past (FR-005)

```gherkin
Given an active event "DevConf"
When an organizer creates a coupon with expiry "2020-01-01T00:00Z"
Then the coupon creation is rejected with reason "expiry must be in the future"
```

#### SC-006: Rejected — cancelled event (FR-006)

```gherkin
Given event "OldConf" has been cancelled
When an organizer creates a coupon for "OldConf"
Then the coupon creation is rejected with reason "event not active"
```

### Coupon Listing

#### SC-007: List coupons for an event (FR-007)

```gherkin
Given an active event "DevConf" with the following coupons:
  | email                | status   | ticket types  |
  | speaker@example.com  | active   | Speaker Pass  |
  | alice@example.com    | redeemed | General       |
  | bob@example.com      | revoked  | Workshop      |
When an organizer lists coupons for "DevConf"
Then all 3 coupons are returned with their status, email, ticket types, and expiry
```

#### SC-008: Empty coupon list (FR-007)

```gherkin
Given an active event "DevConf" with no coupons
When an organizer lists coupons for "DevConf"
Then an empty list is returned
```

### Coupon Details

#### SC-009: View coupon details (FR-008)

```gherkin
Given a coupon for "speaker@example.com" on event "DevConf"
When an organizer views the coupon details
Then the full details are returned including the coupon code
```

### Coupon Revocation

#### SC-010: Successful revocation (FR-009)

```gherkin
Given an active coupon "INVITE-001" for "speaker@example.com"
When an organizer revokes coupon "INVITE-001"
Then the coupon status changes to "revoked"
  And the coupon can no longer be used for registration
```

#### SC-011: Revoke already-expired coupon — succeeds (FR-009)

```gherkin
Given an expired coupon "INVITE-002" for "bob@example.com"
When an organizer revokes coupon "INVITE-002"
Then the coupon status changes to "revoked"
```

#### SC-012: Rejected — revoke redeemed coupon (FR-010)

```gherkin
Given a redeemed coupon "INVITE-003" for "alice@example.com"
When an organizer attempts to revoke coupon "INVITE-003"
Then the revocation is rejected with reason "coupon already redeemed"
```

## 5. Domain Model

### 5.1 Entities

#### Coupon (aggregate root — **new**)

Represents a single-use invitation to register for an event with specific ticket
types. Coupons bypass capacity and email domain restrictions. They optionally
bypass the registration window.

| Attribute                | Type           | Constraints                        | Description                              |
| ------------------------ | -------------- | ---------------------------------- | ---------------------------------------- |
| id                       | UUID           | PK, generated                      |                                          |
| eventId                  | UUID           | Required, cross-module reference   | Event this coupon is for                 |
| code                     | CouponCode     | Required, unique, generated        | GUID-based lookup key                    |
| email                    | string         | Required, normalized to lowercase  | Target attendee email                    |
| allowedTicketTypeIds     | List\<UUID\>   | At least one                       | Ticket types the attendee can choose from |
| expiresAt                | datetime       | Required, must be future at creation | When the coupon expires                 |
| bypassRegistrationWindow | boolean        | Required, default false            | Whether this coupon bypasses the window  |
| redeemedAt               | datetime?      | Nullable                           | Set when used for registration           |
| revokedAt                | datetime?      | Nullable                           | Set when revoked by organizer            |
| createdAt                | datetime       | Generated, immutable               |                                          |

### 5.2 Relationships

- A **Coupon** belongs to one event (via eventId, cross-module reference to
  Organization module).
- A **Coupon** allowlists one or more ticket types (via allowedTicketTypeIds,
  cross-module reference).
- A **Registration** (FEAT-004) optionally references a **Coupon** (via couponId).

### 5.3 Value Objects

#### CouponCode (existing)

| Attribute | Type | Constraints      |
| --------- | ---- | ---------------- |
| value     | GUID | Unique, generated |

#### CouponStatus (derived, not persisted)

Enum: `Active`, `Redeemed`, `Revoked`, `Expired`

Status is computed from aggregate state:

- **Redeemed** if `redeemedAt` is set
- **Revoked** if `revokedAt` is set
- **Expired** if `expiresAt` < now (and not redeemed or revoked)
- **Active** otherwise

Evaluation order matters: Redeemed > Revoked > Expired > Active.

### 5.4 Domain Rules and Invariants

1. **At least one ticket type**: A coupon must allowlist at least one ticket type.
2. **Valid ticket types**: All allowlisted ticket types must exist and not be
   cancelled at creation time.
3. **Future expiry**: Expiry datetime must be in the future at creation time.
4. **Active events only**: Coupons cannot be created for cancelled or archived
   events.
5. **Single-use**: A coupon can be redeemed at most once (`redeemedAt` set once,
   never cleared).
6. **Revoke guards**: Only non-redeemed coupons can be revoked. Revoking an
   already-revoked or expired coupon is idempotent.
7. **Immutable after creation**: Coupon attributes (email, ticket types, expiry,
   bypass flag) cannot be modified after creation.

## 6. Non-Functional Requirements

| ID      | Category        | Requirement                                                                                                                             |
| ------- | --------------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Authorization   | Coupon management endpoints (create, list, view, revoke) require Owner or Organizer role.                                               |
| NFR-002 | Module boundary | Coupons are persisted in the Registrations module's schema. Ticket type validation reads from `IOrganizationFacade`. No cross-module DbContext access. |
| NFR-003 | Idempotency     | Revoking an already-revoked or expired coupon succeeds without error.                                                                   |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                         | Expected Behavior                                                                                                                                  |
| ----- | ---------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| EC-01 | Coupon created for email that already has a registration         | Coupon is created — the duplicate guard fires at registration time (FEAT-004 FR-016), not at coupon creation.                                      |
| EC-02 | Ticket type cancelled after coupon creation but before use       | Coupon remains in the system. Registration rejects it — cancelled ticket types are always rejected (FEAT-004 FR-013).                              |
| EC-03 | Event cancelled after coupon creation                            | Coupon remains in the system. Registration rejects — event must be active (FEAT-004 FR-015). Coupon list still shows the coupon.                   |
| EC-04 | Organizer views coupon for event they no longer have access to   | Rejected — authorization check at endpoint level.                                                                                                  |
| EC-05 | Coupon code collision                                            | Extremely unlikely with GUIDs. If it occurs, DB unique constraint rejects creation; retry generates a new code.                                    |
| EC-06 | Revoke and registration arrive simultaneously                    | Either: coupon is revoked first (registration fails) or registration redeems first (revoke fails with "already redeemed"). Optimistic concurrency on Coupon aggregate ensures exactly one succeeds. |

## 8. Success Criteria

| ID   | Criterion                                                                          |
| ---- | ---------------------------------------------------------------------------------- |
| S-01 | All 12 acceptance scenarios pass in CI.                                            |
| S-02 | Organizer can create a coupon and see the generated code in the response.          |
| S-03 | Invitation email is triggered upon coupon creation.                                |
| S-04 | Coupon list correctly shows derived status (active/redeemed/revoked/expired).      |
| S-05 | Revoked coupons are rejected at registration time (verified via FEAT-004 flow).    |
| S-06 | Concurrent revoke-vs-redeem race resolved correctly by optimistic concurrency.     |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **FEAT-003 (Event Management)**: Events and ticket types must exist. Ticket type
  validation at coupon creation reads from the Organization module.
- **FEAT-004 (Attendee Registration)**: Coupon redemption flow is defined in
  FEAT-004. This spec defines the entity that FEAT-004 references.
- **Organization Facade**: `IOrganizationFacade` must expose ticket type data
  (existence and cancellation status) to the Registrations module.
- **Email Module (future)**: Invitation email delivery depends on a separate email
  module. This spec triggers the email; delivery mechanics are out of scope.

### 9.2 Constraints

- Coupons are persisted in the Registrations module's schema (not the Organization
  module).
- Cross-module data access uses the facade pattern or module events, never direct
  DbContext access (ADR-001).
- Coupon attributes are immutable after creation — no update endpoint.

### 9.3 Architecture References

| Arc42 Section                    | Relevance to This Feature                                                    |
| -------------------------------- | ---------------------------------------------------------------------------- |
| 5. Building Block View           | Registrations module structure, Coupon aggregate                             |
| 6. Runtime View                  | Coupon creation flow, cross-module facade call for ticket type validation    |
| 8. Crosscutting Concepts         | Optimistic concurrency, facade pattern, domain event taxonomy                |
| 9. Architecture Decisions (ADRs) | ADR-001 (modular monolith), ADR-002 (feature-sliced endpoints)              |
| 10. Quality Requirements         | Authorization, module boundary enforcement                                   |

## 10. Open Questions

_No open questions._

