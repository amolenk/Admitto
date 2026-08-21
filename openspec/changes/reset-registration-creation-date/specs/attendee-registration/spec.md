## ADDED Requirements

### Requirement: Resetting a cancelled registration refreshes its creation timestamp
When any registration channel resets a `Cancelled` `Registration`, the system SHALL preserve the existing registration ID, restore its registered state using the channel's supplied registration time, and set the aggregate's `CreatedAt` to that same time.

This requirement applies to public self-service registration, coupon registration, and admin-add registration because they share the `Registration` aggregate reset operation.
The reset SHALL continue to clear cancellation and reconfirmation state, replace the attendee's current registration data, and publish the existing attendee-registered side effects using the same reset time.

#### Scenario: Self-service reset refreshes creation timestamp
- **WHEN** a cancelled registration is successfully reset through public self-service registration at time "2026-08-12T10:00Z"
- **THEN** the aggregate retains its original registration ID, has `Status=Registered`, and has `CreatedAt="2026-08-12T10:00Z"`

#### Scenario: Coupon reset refreshes creation timestamp
- **WHEN** a cancelled registration is successfully reset through coupon registration at time "2026-08-12T10:00Z"
- **THEN** the aggregate retains its original registration ID, has `Status=Registered`, and has `CreatedAt="2026-08-12T10:00Z"`

#### Scenario: Failed registration attempt does not refresh creation timestamp
- **WHEN** a self-service or coupon registration attempt fails before resetting a cancelled registration
- **THEN** the cancelled aggregate retains its existing `CreatedAt` and no attendee-registered side effects are produced
