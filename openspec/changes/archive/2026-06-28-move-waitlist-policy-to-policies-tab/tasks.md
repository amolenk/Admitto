## 1. Backend Domain And Persistence

- [x] 1.1 Add a required `TicketedEventWaitlistPolicy` value object with `QuietHoursStart`, `QuietHoursEnd`, and defaults of `22:00` / `08:00`.
- [x] 1.2 Replace scalar `TicketedEvent.QuietHoursStart` / `QuietHoursEnd` usage with `TicketedEvent.WaitlistPolicy` and a `ConfigureWaitlistPolicy` mutator guarded by active-event status.
- [x] 1.3 Update waitlist coupon expiry calculation call sites to read quiet hours from `ticketedEvent.WaitlistPolicy` without changing expiry behavior.
- [x] 1.4 Update EF mapping so the value object uses `OwnsOne` and persists to `waitlist_policy_quiet_hours_start` and `waitlist_policy_quiet_hours_end`, mirroring the other `TicketedEvent` policy columns.
- [x] 1.5 Generate and review the EF schema change through official tooling; because the product is not live, no data-preserving migration from the old quiet-hours columns is required.

## 2. Backend API Contracts And Slices

- [x] 2.1 Update event details DTO/query mapping to return a required nested `waitlistPolicy` object instead of top-level quiet-hours fields.
- [x] 2.2 Remove quiet-hours fields from the general event details update request/command/handler contract.
- [x] 2.3 Add a `ConfigureWaitlistPolicy` use-case slice with command, handler, admin request DTO, validator, and endpoint-owned unit-of-work commit.
- [x] 2.4 Register the waitlist-policy endpoint in the Registrations admin endpoint wiring, following the existing `registration-policy` and `reconfirm-policy` route style.
- [x] 2.5 Add or update backend tests for default policy values, waitlist-policy updates, archived-event rejection, event details shape, and unchanged quiet-hours expiry behavior.

## 3. Admin UI And SDK

- [x] 3.1 Start Aspire with `aspire start --isolated`, wait for `api`, fetch `/openapi/v1.json`, and regenerate the Admin UI SDK before writing UI calls.
- [x] 3.2 Remove quiet-hours fields, validation, change detection, and payload properties from the General settings form.
- [x] 3.3 Add a Waitlist policy form/section to the Policies tab using generated API functions and `event.waitlistPolicy` values.
- [x] 3.4 Ensure the Waitlist policy form submits `expectedVersion`, invalidates event queries after save, and surfaces validation/concurrency errors consistently with other policy forms.
- [x] 3.5 Ensure archived events disable the Waitlist policy form and display the existing policy read-only banner.
- [x] 3.6 Update UI copy to state that notifications are sent immediately and quiet hours extend claim deadlines.

## 4. Documentation And Verification

- [x] 4.1 Update arc42 documentation if the policy model, API contract, or runtime flow changes need architectural documentation.
- [x] 4.2 Run `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj` first and fix any architecture violations.
- [x] 4.3 Run targeted domain tests for `TicketedEventWaitlistPolicy` and `WaitlistClaimWindowCalculator` behavior.
- [x] 4.4 Run targeted Registrations integration tests for event details, waitlist-policy updates, and waitlist notification expiry.
- [x] 4.5 Run targeted Admin UI checks for type generation/build/lint or the existing UI test command relevant to the changed forms.
- [x] 4.6 Review generated OpenAPI/SDK diffs and ensure no handwritten API client/proxy replacement was introduced.
