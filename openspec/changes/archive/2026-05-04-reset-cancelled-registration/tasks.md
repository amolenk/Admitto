## 1. Domain Model

- [x] 1.1 Add a `Registration.Reset(...)` domain method that only works for `Cancelled` registrations.
- [x] 1.2 Ensure reset preserves `RegistrationId`, `TeamId`, `EventId`, and `Email`.
- [x] 1.3 Ensure reset sets `Status` to `Registered`, clears `CancellationReason`, clears `HasReconfirmed`/`ReconfirmedAt`, replaces attendee details, replaces ticket snapshots, and replaces additional details.
- [x] 1.4 Ensure reset raises `AttendeeRegisteredDomainEvent` with the current attendee and ticket data.
- [x] 1.5 Add domain tests for successful reset, reset of a non-cancelled registration, cleared cancellation/reconfirmation state, replaced data, and emitted domain event.

## 2. Registration Handler

- [x] 2.1 Update `RegisterAttendeeHandler` to load an existing registration by `EventId` and `Email` after mode-specific access gates remain satisfied.
- [x] 2.2 Reject an existing `Registered` registration with the existing already-registered conflict semantics.
- [x] 2.3 For an existing `Cancelled` registration, run the existing event/catalog/ticket/additional-detail/capacity/coupon flow and call `Registration.Reset(...)` instead of adding a new row.
- [x] 2.4 Keep self-service email verification as the first handler check before any event, catalog, coupon, or registration lookup.
- [x] 2.5 Keep coupon validation and redemption semantics intact, including redeeming the coupon only after all reset gates pass.
- [x] 2.6 Keep the existing create-new-registration path for event/email pairs with no existing registration.

## 3. Integration Tests

- [x] 3.1 Extend the registration fixture to seed cancelled registrations with configurable previous attendee data, tickets, additional details, cancellation reason, and reconfirmation state.
- [x] 3.2 Update duplicate-active tests for self-service and admin-add to assert the business conflict semantics rather than relying only on the database constraint.
- [x] 3.3 Add self-service reset tests covering preserved registration id, updated attendee/tickets/details, cleared cancellation/reconfirmation state, capacity claim, and attendee-registered event side effects.
- [x] 3.4 Add coupon reset tests covering preserved registration id, coupon redemption, coupon bypass capacity behavior, cleared cancellation/reconfirmation state, and attendee-registered event side effects.
- [x] 3.5 Add admin-add reset tests covering preserved registration id, admin capacity bypass behavior, updated data, cleared cancellation/reconfirmation state, and attendee-registered event side effects.
- [x] 3.6 Add a failing-gate reset test proving a cancelled registration remains cancelled and capacity is not consumed when a self-service reset request fails an existing gate such as a closed registration window.

## 4. Verification

- [x] 4.1 Run `dotnet test tests/Admitto.Module.Registrations.Domain.Tests/Admitto.Module.Registrations.Domain.Tests.csproj`.
- [x] 4.2 Run `dotnet test tests/Admitto.Module.Registrations.Tests/Admitto.Module.Registrations.Tests.csproj`.
- [x] 4.3 Confirm no Admin UI SDK regeneration or CLI changes are required because this change does not alter HTTP contract shape and the CLI is legacy.
