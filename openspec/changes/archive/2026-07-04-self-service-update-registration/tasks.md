## 1. Domain And Application Model

- [x] 1.1 Add or update a `Registration` domain operation that replaces first name, last name, additional details, and ticket snapshots while rejecting cancelled registrations.
- [x] 1.2 Ensure ticket-change domain side effects are raised only when the final ticket selection differs from the current ticket selection.
- [x] 1.3 Create a new/different Registrations use-case slice for partner registration updates, for example `Application/UseCases/Registrations/UpdatePartnerRegistration/`; do not widen the existing `ChangeAttendeeTickets` slice.
- [x] 1.4 Add the new slice command carrying `FirstName`, `LastName`, final `TicketTypeIds`, optional `AdditionalDetails`, and optional `WaitlistCouponCode`.
- [x] 1.5 Implement the new slice handler to load `TicketedEvent`, validate registration window/status, validate names and additional details, apply ticket capacity delta, redeem waitlist coupons, and persist all registration changes atomically.

## 2. Partner API Contract

- [x] 2.1 Replace the Partner route `PUT /api/events/{eventSlug}/registrations/{registrationId}/tickets` with `PUT /api/events/{eventSlug}/registrations/{registrationId}`.
- [x] 2.2 Replace the Partner request DTO with the full update payload: `firstName`, `lastName`, `ticketTypeIds`, optional `additionalDetails`, and optional `waitlistCouponCode`.
- [x] 2.3 Update Partner request validation for required names, required ticket selection, additional-details shape, and waitlist coupon parsing.
- [x] 2.4 Update endpoint registration and generated OpenAPI metadata by removing the ticket-only public contract.

## 3. Tests

- [x] 3.1 Add domain tests for replacing attendee details and preserving cancelled-registration invariants.
- [x] 3.2 Add handler integration tests under the new slice for successful full update, details-only update without ticket-change event, additional-detail validation failures, capacity/coupon failures leaving attendee details unchanged, and self-service-disabled ticket rejection.
- [x] 3.3 Update API tests for the new route, required-field validation, missing API key, not found scoping, cancelled registration, and removal of the old `/tickets` contract.

## 4. Documentation And Verification

- [x] 4.1 Update arc42 runtime/cross-cutting documentation if endpoint behavior or registration-bound mutation semantics change materially.
- [x] 4.2 Run architecture tests first: `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj`.
- [x] 4.3 Run targeted Registrations domain, integration, and API tests for the changed self-service update behavior.
