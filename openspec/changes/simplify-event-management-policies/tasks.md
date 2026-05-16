## 1. Domain — Remove Cancellation Policy

- [x] 1.1 Remove `CancellationPolicy` value object and all references from `TicketedEvent` aggregate
- [x] 1.2 Remove `SetCancellationPolicy` command, handler, and endpoint from Registrations module
- [x] 1.3 Delete `CancellationPolicyEntityConfiguration` and remove its mapping from `RegistrationsDbContext`
- [x] 1.4 Add EF Core migration: drop `cancellation_policies` table (or equivalent owned-entity columns)
- [x] 1.5 Update domain/integration event payloads that include cancellation policy fields to omit them

## 2. Domain — Remove Cancelled Event & Ticket Type Status

- [x] 2.1 Remove `Cancelled` value from `EventLifecycleStatus` enum; simplify to `Active` and `Archived` only
- [x] 2.2 Remove `CancelEvent` command, handler, and endpoint from Registrations module
- [x] 2.3 Remove `TicketedEventCancelled` integration event class and all publishers/subscribers
- [x] 2.4 Remove `CancelTicketType` command, handler, and endpoint from Registrations module
- [x] 2.5 Remove `Cancelled` from `TicketCatalogEventStatus` / `TicketCatalog` event status; update `TicketCatalog` aggregate accordingly
- [x] 2.6 Add EF Core migration: migrate any existing `Cancelled` events to `Archived`; migrate cancelled ticket types (decide per OQ1 in design: recommend set to Active)
- [x] 2.7 Update `ArchiveEvent` command guard — previously required `Active` OR `Cancelled`; now `Active` only

## 3. Domain — Self-Service Cancel Guard

- [x] 3.1 Add hard-coded pre-condition in `CancelRegistration` domain operation: reject if `now >= event.StartsAt` (HTTP 409 "event has already started")
- [x] 3.2 Ensure `TicketedEvent.StartsAt` is accessible in the Registrations aggregate at the point of cancellation
- [x] 3.3 Add domain test: self-service cancel fails when event has started
- [x] 3.4 Add domain test: self-service cancel succeeds when event has not yet started

## 4. Domain — Reconfirm Policy: Add MinEmailInterval

- [x] 4.1 Add `MinEmailInterval` (TimeSpan / hours) property to `TicketedEventReconfirmPolicy` value object
- [x] 4.2 Update `SetReconfirmPolicy` command and validator: require `MinEmailInterval >= 1 hour`
- [x] 4.3 Include `MinEmailInterval` in the `TicketedEventReconfirmPolicyChanged` integration event payload
- [x] 4.4 Add EF Core migration: add `min_email_interval_hours` column to reconfirm policy table/owned entity

## 5. Email Module — Per-Attendee MinEmailInterval Throttling

- [x] 5.1 Add `CreatedAt` (`DateTimeOffset`) to `RegistrationListItemDto` and populate it from the Registrations query
- [x] 5.2 Update `EvaluateReconfirmJob` to accept `MinEmailInterval` from the stored policy
- [x] 5.3 Query `email_log` in bulk (per-event) for `reconfirm` rows to get per-attendee last-send times
- [x] 5.4 Filter candidate attendees: exclude any whose `max(registration.CreatedAt, lastReconfirmSentAt) + MinEmailInterval > now` (using `CreatedAt` from the existing `QueryRegistrationsAsync` result)
- [x] 5.5 Update `TicketedEventReconfirmPolicyChanged` handler in Email module to persist new `MinEmailInterval` value
- [x] 5.6 Remove `TicketedEventCancelled` handler from Email module (trigger removal now only on `TicketedEventArchived`)
- [x] 5.7 Add integration test: attendee excluded when registered less than MinEmailInterval ago
- [x] 5.8 Add integration test: attendee excluded when last reconfirm email sent less than MinEmailInterval ago
- [x] 5.9 Add integration test: attendee included once MinEmailInterval has elapsed since last email

## 6. API — Remove Endpoints and Update OpenAPI Spec

- [x] 6.1 Confirm that `PUT /events/{teamSlug}/{eventSlug}/policies/cancellation` endpoint is removed (from step 1.2)
- [x] 6.2 Confirm that `POST /events/{teamSlug}/{eventSlug}/cancel` and `POST /ticket-types/{id}/cancel` endpoints are removed (from step 2.2/2.4)
- [x] 6.3 Verify `GET /events` and `GET /events/{slug}` responses no longer include `cancellationPolicy` or `Cancelled` status
- [x] 6.4 Verify `PUT /events/{teamSlug}/{eventSlug}/policies/reconfirm` request schema includes `minEmailIntervalHours`

## 7. Admin UI — Remove Cancellation Policy Page

- [x] 7.1 Delete the Cancellation Policy settings tab/page from the Admin UI event settings
- [x] 7.2 Remove "Cancellation" entry from the event settings sidebar navigation
- [x] 7.3 Remove any read-only display of cancellation policy fields from event detail views

## 8. Admin UI — Remove Cancel Event / Cancel Ticket Type Actions

- [x] 8.1 Remove "Cancel Event" action/button from event management UI
- [x] 8.2 Remove "Cancel Ticket Type" action from ticket type cards
- [x] 8.3 Remove `Cancelled` status badge/label from the events list and event detail header
- [x] 8.4 Update archive action guard in the UI to only require `Active` status (remove check for `Cancelled`)

## 9. Admin UI — Add MinEmailInterval to Reconfirm Policy Form

- [x] 9.1 Add `minEmailIntervalHours` numeric input to the Reconfirm Policy form (label suggestion: "Minimum hours between emails")
- [x] 9.2 Add client-side validation: `minEmailIntervalHours` must be ≥ 1
- [x] 9.3 Include `minEmailIntervalHours` in the form submission payload to the backend
- [x] 9.4 Display current `minEmailIntervalHours` value when loading existing policy in the form

## 10. Admin UI SDK Regeneration

- [x] 10.1 Regenerate the Admin UI OpenAPI SDK after all backend endpoint changes are complete (`aspire start --isolated` → `curl spec` → `pnpm openapi-ts`)
- [x] 10.2 Update all Admin UI proxy routes and components to use the regenerated SDK functions

## 11. Architecture & Regression Tests

- [x] 11.1 Run `dotnet test tests/Admitto.Core.ArchTests/` — fix any architecture violations introduced by removals
- [x] 11.2 Run `dotnet test tests/Admitto.Core.DomainTests/` — ensure all domain tests pass
- [x] 11.3 Run `dotnet test tests/Admitto.Core.IntegrationTests/` — ensure module integration tests pass
- [x] 11.4 Run `dotnet test tests/Admitto.Api.Tests/` — ensure E2E API tests pass (including new cancel-after-start scenario)
- [x] 11.5 Run `cd src/Admitto.UI.Admin && pnpm build` — ensure Admin UI compiles without TypeScript errors
