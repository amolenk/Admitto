## 1. Request/Response Contracts

- [x] 1.1 Update the public self-service registration request to carry `registerTicketTypeIds` and `waitlistTicketTypeIds` instead of one ambiguous ticket list.
- [x] 1.2 Add a public registration submission response that reports `registrationId`, `registeredTicketTypeIds`, and `waitlistedTicketTypeIds`.
- [x] 1.3 Extend the public self-service ticket-change request with optional waitlist coupon information.
- [x] 1.4 Update validators for explicit registration, waitlist, and coupon-claim request fields.

## 2. Registration Submission Behavior

- [x] 2.1 Update the self-service registration command/handler to validate registration tickets and waitlist tickets as separate explicit actions.
- [x] 2.2 Claim capacity only for `registerTicketTypeIds` and create/update the `Registration` only when at least one registration ticket is requested.
- [x] 2.3 Create waitlist entries for `waitlistTicketTypeIds` in the same unit of work as any registration changes.
- [x] 2.4 Reject stale ticket-state mismatches atomically, including requested registration tickets that entered WaitlistMode and requested waitlist tickets that left WaitlistMode.
- [x] 2.5 Preserve duplicate-email rejection for creating a new registration while still allowing waitlist-only submissions when the email has no active registration.

## 3. Waitlist Coupon Ticket Change

- [x] 3.1 Update coupon validation so a waitlist coupon can be used as a capacity grant for self-service ticket change.
- [x] 3.2 Ensure the final ticket selection includes the waitlist coupon's offered ticket type.
- [x] 3.3 Bypass capacity and WaitlistMode only for the coupon-backed ticket; enforce normal self-service checks for all other newly claimed tickets.
- [x] 3.4 Mark the coupon and corresponding waitlist coupon as redeemed in the same transaction as the ticket change.
- [x] 3.5 Keep coupon-backed first-registration behavior working for attendees without an active registration.

## 4. Tests

- [x] 4.1 Add handler/integration tests for mixed registration + waitlist submission success.
- [x] 4.2 Add tests for waitlist-only submission with `registrationId = null` outcome.
- [x] 4.3 Add tests for stale ticket-state rejection with no partial persistence.
- [x] 4.4 Add tests proving waitlisted tickets may overlap current registered tickets and other waitlisted tickets.
- [x] 4.5 Add tests for waitlist coupon ticket-change success against an existing registration.
- [x] 4.6 Add tests for invalid waitlist coupon ticket-change requests, including missing offered ticket and final overlapping ticket set.
- [x] 4.7 Add API tests for updated public request/response contracts and validation failures.

## 5. API Clients And Verification

- [x] 5.1 Regenerate affected generated API clients from the updated OpenAPI spec before using changed contracts in UI/proxy code.
- [x] 5.2 Run `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj`.
- [x] 5.3 Run targeted Registrations integration and API tests for the changed public registration, waitlist, coupon, and ticket-change flows.
