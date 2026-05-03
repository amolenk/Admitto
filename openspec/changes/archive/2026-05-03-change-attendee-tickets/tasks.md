## 1. Refactor — Fold ticket-selection validation into `TicketCatalog.Claim`

- [x] 1.1 Move the four validation checks (duplicate slugs, unknown slugs, cancelled slugs, overlapping time slots) from `RegisterAttendeeHandler.ValidateTicketTypeSelection` into the top of `TicketCatalog.Claim`, before the capacity loop; build the `ticketTypeMap` once inside `Claim` and reuse it for both validation and claiming
- [x] 1.2 Add `DuplicateTicketTypes`, `UnknownTicketTypes`, `CancelledTicketTypes`, and `OverlappingTimeSlots` error factories to `TicketCatalog.Errors` (use `ticket_catalog.*` error codes)
- [x] 1.3 Delete `RegisterAttendeeHandler.ValidateTicketTypeSelection` private method and its four corresponding error factories (`DuplicateTicketTypes`, `UnknownTicketTypes`, `CancelledTicketTypes`, `OverlappingTimeSlots`) from `RegisterAttendeeHandler.Errors`; remove the `ValidateTicketTypeSelection(...)` call site and the `ticketTypeMap` variable that was only needed for it
- [x] 1.4 Remove `Registration.EnsureNoDuplicateSlugs` private method and `Registration.Errors.DuplicateTicketTypes` (duplicate guard is now solely in `Claim`); remove the call in `Registration.Create`
- [x] 1.5 Verify `TicketCatalog.Claim` and `TicketCatalog.Release` handle empty slug lists without error (no-op delta case)
- [x] 1.6 Update existing `RegisterAttendee` unit tests whose expected error codes changed from `registration.*` to `ticket_catalog.*`

## 2. Domain — New Event & Registration Method

- [x] 2.1 Add `TicketsChangedDomainEvent` in `Domain/DomainEvents/` carrying `teamId`, `ticketedEventId`, `registrationId`, `recipientEmail`, `firstName`, `lastName`, `oldTickets` (IReadOnlyList\<TicketTypeSnapshot\>), `newTickets` (IReadOnlyList\<TicketTypeSnapshot\>), and `changedAt` (DateTimeOffset)
- [x] 2.2 Add `Registration.ChangeTickets(IReadOnlyList<TicketTypeSnapshot> newTickets, DateTimeOffset changedAt)`: replace `_tickets`, raise `TicketsChangedDomainEvent`; add `Errors.RegistrationIsCancelled`

## 3. Application — Change Tickets Command & Handler

- [x] 3.1 Add `ChangeMode` enum (`Admin`) and `ChangeAttendeeTicketsCommand` record (`EventId`, `RegistrationId`, `TicketTypeSlugs`, `Mode`) in `Application/UseCases/Registrations/ChangeAttendeeTickets/`
- [x] 3.2 Implement `ChangeAttendeeTicketsHandler`: load registration → reject if Cancelled → load event → reject if not Active → load catalog → compute delta (toRelease = current ∖ new, toClaim = new ∖ current) → `catalog.Release(toRelease)` → `catalog.Claim(toClaim, enforce: false)` (validation runs inside Claim) → build new snapshots → `registration.ChangeTickets(snapshots, now)`

## 4. Contracts — Integration Event

- [x] 4.1 Add `AttendeeTicketsChangedIntegrationEvent` in `Admitto.Module.Registrations.Contracts/IntegrationEvents/` carrying `teamId`, `ticketedEventId`, `registrationId`, `recipientEmail`, `firstName`, `lastName`, `newTickets` (list of `{Slug, Name}`), and `changedAt`

## 5. Application — Outbox & Activity Log Event Handlers

- [x] 5.1 Add `PublishTicketsChangedIntegrationEventHandler` (domain event → outbox) in `Application/UseCases/Registrations/ChangeAttendeeTickets/EventHandlers/`
- [x] 5.2 Add `TicketsChanged` to the `ActivityType` enum
- [x] 5.3 Add `WriteTicketsChangedActivityLogHandler` in `Application/UseCases/Registrations/WriteActivityLog/EventHandlers/` projecting `TicketsChangedDomainEvent` → `ActivityLog` row with `activity_type=TicketsChanged` and `metadata` = JSON `{"from":[...],"to":[...]}`
- [x] 5.4 Confirm `activityType` is serialized as a string in `GetRegistrationDetailsHandler` response (no code change needed if it is; update mapping if not)

## 6. Admin HTTP Endpoint

- [x] 6.1 Add `ChangeAttendeeTicketsHttpRequest` (`TicketTypeSlugs: string[]`), `ChangeAttendeeTicketsValidator` (non-empty array, no blank slugs), and `ChangeAttendeeTicketsHttpEndpoint` mapped to `PUT /admin/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}/tickets`
- [x] 6.2 Register the endpoint in the module's admin endpoint registration entry point

## 7. Email Module — Ticket-Change & Updated Registration Email

- [x] 7.1 Add `AttendeeTicketsChangedIntegrationEventHandler` sending a `ticket`-type email with idempotency key `tickets-changed:{registrationId}:{changedAt-unix-ms}` and `ticket_types` = new ticket names
- [x] 7.2 Add `Tickets` (list of `{Slug, Name}`) to `AttendeeRegisteredIntegrationEvent` and update the outbox handler that publishes it to include the ticket snapshot
- [x] 7.3 Update `AttendeeRegisteredIntegrationEventHandler` to pass `ticket_types` from the integration event's `Tickets` field to `SendEmailCommand`

## 8. Email Templates — Add Ticket Types

- [x] 8.1 Update `ticket.html` to show a `{{ if ticket_types && ticket_types.size > 0 }}...{{ end }}` block listing ticket type names
- [x] 8.2 Update `ticket.txt` similarly

## 9. Admin UI

- [x] 9.1 Remove the "Change ticket types" button at the top of the attendee detail page
- [x] 9.2 Wire the "Change" button in the Tickets card to call `PUT .../tickets`; implement the ticket selection UI (reuse existing ticket-type picker pattern)
- [x] 9.3 Regenerate the Admin UI SDK after the backend endpoint is live: `cd src/Admitto.UI.Admin && pnpm run openapi-ts`

## 10. Tests

- [x] 10.1 Unit tests for `TicketCatalog.Claim` validation: duplicate slugs, unknown slugs, cancelled slugs, overlapping time slots, empty list no-op
- [x] 10.2 Unit tests for `Registration.ChangeTickets`: happy path, no-op same selection, cancelled registration rejected
- [x] 10.3 Unit tests for `ChangeAttendeeTicketsHandler`: SC001 (delta capacity), SC002 (sold-out no block), SC004 (cancelled), SC005 (event not active)
- [x] 10.4 Unit test for `WriteTicketsChangedActivityLogHandler`: SC007 metadata JSON shape
- [x] 10.5 E2E API test: SC013 (organizer can change tickets) and SC014 (non-member forbidden)
