## 1. Email Module — Add `registrationId` to EmailLog

- [x] 1.1 Add nullable `RegistrationId` property (`Guid?`) to `EmailLog` domain entity (`src/Admitto.Module.Email/Domain/Entities/EmailLog.cs`); update `EmailLog.Create(...)` factory method to accept an optional `registrationId` parameter
- [x] 1.2 Update `SendEmailCommand` (`src/Admitto.Module.Email/Application/UseCases/SendEmail/SendEmailCommand.cs`) to include an optional `RegistrationId` (`Guid?`) property
- [x] 1.3 Update `SendEmailCommandHandler` to pass `command.RegistrationId` when calling `EmailLog.Create(...)` 
- [x] 1.4 Update `AttendeeRegisteredIntegrationEventHandler` to pass the `RegistrationId` from the integration event payload in the `SendEmailCommand`
- [x] 1.5 Update `MailKitBulkSmtpSender` (or wherever bulk fan-out creates `EmailLog` rows) to pass `BulkEmailRecipient.RegistrationId` when creating each `EmailLog` row
- [x] 1.6 Add EF Core migration for the `registration_id` column (nullable `uuid`) on the `email_log` table
- [x] 1.7 Update the `EmailLog` EF entity configuration to map the new property

## 2. Registrations Module — ActivityLog Projection

- [x] 2.1 Create `ActivityLog` entity in `src/Admitto.Module.Registrations/Domain/Entities/ActivityLog.cs` with fields: `Id`, `RegistrationId`, `ActivityType` (enum: `Registered`, `Reconfirmed`, `Cancelled`), `OccurredAt`, `Metadata` (nullable string)
- [x] 2.2 Add `ActivityType` enum alongside the entity (or in a domain enums file)
- [x] 2.3 Add `DbSet<ActivityLog>` to `RegistrationsDbContext` and create EF entity configuration (`activity_log` table, index on `(registration_id, activity_type, occurred_at)` for idempotency checks)
- [x] 2.4 Add EF Core migration for the `activity_log` table
- [x] 2.5 Implement `AttendeeRegisteredActivityLogHandler` (domain event handler for `AttendeeRegisteredDomainEvent`): insert `ActivityType=Registered` entry
- [x] 2.6 Implement `RegistrationReconfirmedActivityLogHandler` (domain event handler for `RegistrationReconfirmedDomainEvent`): insert `ActivityType=Reconfirmed` entry
- [x] 2.7 Implement `RegistrationCancelledActivityLogHandler` (domain event handler for `RegistrationCancelledDomainEvent`): insert `ActivityType=Cancelled` entry with `Metadata` set to the cancellation reason string

## 3. Backend — Registration Detail (Registrations Module)

- [x] 3.1 Create `ActivityLogEntryDto` record with fields: `activityType` (string), `occurredAt`, `metadata` (nullable string)
- [x] 3.2 Create `RegistrationDetailDto` record with all fields: `id`, `email`, `firstName`, `lastName`, `status`, `registeredAt`, `hasReconfirmed`, `reconfirmedAt`, `cancellationReason`, `tickets` (list of `{slug, name}`), `additionalDetails` (dictionary), `activities` (list of `ActivityLogEntryDto`, oldest first)
- [x] 3.3 Create `GetRegistrationDetailsQuery` with `TeamId`, `EventId`, and `RegistrationId`
- [x] 3.4 Implement `GetRegistrationDetailsHandler` that loads the registration by ID scoped to teamId+eventId, fetches associated `ActivityLog` entries ordered by `OccurredAt` ascending, maps to `RegistrationDetailDto`, returns `null` when not found
- [x] 3.5 Create `GetRegistrationDetailsHttpEndpoint` at `GET /admin/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}`, resolve scope via `IOrganizationScopeResolver`, dispatch query, return 200 or 404
- [x] 3.6 Register the new endpoint in the Registrations module's endpoint registration entry point

## 4. Backend — Attendee Emails (Email Module)

- [x] 4.1 Create `AttendeeEmailLogItemDto` record with fields: `id`, `subject`, `emailType`, `status`, `sentAt`, `bulkEmailJobId`
- [x] 4.2 Create `GetAttendeeEmailsQuery` with `TeamId`, `TicketedEventId`, `RegistrationId`
- [x] 4.3 Implement `GetAttendeeEmailsHandler` that queries `email_log` by `(ticketedEventId, registrationId)` ordered by `statusUpdatedAt` descending, returns empty list when no emails found
- [x] 4.4 Create `GetAttendeeEmailsHttpEndpoint` at `GET /admin/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}/emails`, resolve scope via `IOrganizationScopeResolver`, verify registration exists (return 404 if not), dispatch query, return 200
- [x] 4.5 Register the new endpoint in the Email module's endpoint registration entry point

## 5. Admin UI — Attendee Detail Page

- [x] 5.1 Create page file at `src/Admitto.UI.Admin/app/(dashboard)/teams/[teamSlug]/events/[eventSlug]/registrations/[registrationId]/page.tsx`
- [x] 5.2 Add two parallel React Query fetches: registration detail (`GET /admin/…/registrations/{id}`) and attendee emails (`GET /admin/…/registrations/{id}/emails`)
- [x] 5.3 Implement **Hero header card**: avatar initials, name, status badge, reconfirmed badge, email + registered-at row, Cancel and placeholder Change buttons
- [x] 5.4 Implement **Left column — Attendee details card**: `<dl>` grid showing name, email, registration ID, status, reconfirmed; append additional details rows when non-empty
- [x] 5.5 Implement **Left column — Tickets card**: one panel per ticket (name, slug, Active/Released badge); placeholder "Change" button in card header
- [x] 5.6 Implement **Right column — Combined "Activity & emails" timeline**: merge `activities` entries and email log entries into one list sorted by timestamp descending; All/Events/Emails tab filter; coloured icons per entry kind; email entries show placeholder "View" and "Resend" ghost buttons; empty state when list is empty
- [x] 5.7 Implement **Cancel registration** button and dialog: visible when `status=Registered`; dialog shows reason selector (`AttendeeRequest`, `VisaLetterDenied`) with disabled Confirm until reason selected; on confirm calls `POST .../cancel` via `apiClient`; on success invalidates/refetches registration detail; on error shows error notification
- [x] 5.8 Add back link that navigates to the registrations list (`/teams/{teamSlug}/events/{eventSlug}/registrations`)
- [x] 5.9 Add loading skeleton states while fetches are in-flight; add error state when either fetch fails

## 6. Admin UI — Registrations Table Changes

- [x] 6.1 Wrap the attendee name cell in the registrations table with a `<Link>` pointing to the attendee detail page (`/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}`)
- [x] 6.2 Remove the per-row "Cancel" action column, cancel dialog state, and cancel handler from `registrations/page.tsx`

## 7. Tests

- [x] 7.1 Add handler unit tests for `GetRegistrationDetailsHandler` covering SC001–SC008 (happy path, not-found, cancelled, reconfirmed, activities included)
- [x] 7.2 Add unit tests for the three `ActivityLog` domain event handlers covering SC001–SC004 (projection correctness, idempotency)
- [x] 7.3 Add handler unit tests for `GetAttendeeEmailsHandler` covering SC001–SC005 (happy path, empty, cross-event/cross-registration exclusion)
- [x] 7.4 Add E2E API tests for the registration detail endpoint (200 with activities, 403 non-member, 404 not-found)
- [x] 7.5 Add E2E API tests for the attendee emails endpoint (200 with emails populated via `registration_id`, 403 non-member)

