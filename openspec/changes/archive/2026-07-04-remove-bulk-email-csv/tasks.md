# Tasks

## 1. Domain: remove source hierarchy, add Email-owned filter

- [x] 1.1 Add `BulkEmailAttendeeFilter` value object in `Email/Domain/ValueObjects` with fields `TicketTypeIds?`, `RegistrationStatus?`, `HasReconfirmed?`, `RegisteredAfter?`, `RegisteredBefore?`, `AdditionalDetailEquals?`, `RegistrationIds?` (reusing the `Registrations.Contracts.RegistrationStatus` enum).
- [x] 1.2 Delete `Email/Domain/ValueObjects/BulkEmailJobSource.cs` (removes `BulkEmailJobSource`, `AttendeeSource`, `ExternalListSource`).
- [x] 1.3 Delete `Email/Domain/ValueObjects/ExternalListItem.cs`.
- [x] 1.4 In `BulkEmailJob`: replace the `Source` property (and `Create`/`CreateSystemTriggered`/ctor params) with `AttendeeFilter` of type `BulkEmailAttendeeFilter`.
- [x] 1.5 In `BulkEmailRecipient`: make `DisplayName` and `RegistrationId` non-nullable (constructor + properties).
- [x] 1.6 Add/adjust domain tests in `Admitto.Core.DomainTests/Email/Domain/Entities/BulkEmailJobTests.cs` and the `BulkEmailJobBuilder` for the new filter shape and non-null recipient fields.

## 2. Application: resolver, mapping, use cases, reconfirm job

- [x] 2.1 Change `IBulkEmailRecipientResolver.ResolveAsync` to accept `BulkEmailAttendeeFilter`; map it to `QueryRegistrationsDto` inside `BulkEmailRecipientResolver` before the facade call; delete the `switch`, the `ResolveExternalList` method, and the default-throw arm.
- [x] 2.2 In the resolver, assign `DisplayName` directly from first+last name (remove the `IsNullOrWhiteSpace ? null` guard) and pass a non-null `RegistrationId`.
- [x] 2.3 Update `CreateBulkEmailCommand` and `CreateBulkEmailHandler` to carry/use `BulkEmailAttendeeFilter`.
- [x] 2.4 Update `RequestReconfirmationsJob` to construct `BulkEmailAttendeeFilter` instead of `QueryRegistrationsDto` + `AttendeeSource`.
- [x] 2.5 Update `SendBulkEmailJob`: remove the `?? recipient.Email.Value` fallback (`:252`) and collapse `BuildRegistrationLink` to always build the per-registration link (remove the null-registrationId branch).

## 3. HTTP contract

- [x] 3.1 Replace `BulkEmailSourceHttpDto` with a direct attendee-filter request DTO (delete `ExternalListSourceHttpDto`, `ExternalListRecipientHttpDto`); update `CreateBulkEmailHttpRequest` and its `ToCommand`/`ToDomain` mapping to the Email-owned filter.
- [x] 3.2 Update `PreviewBulkEmailHttpRequest`/validator to the direct attendee-filter shape.
- [x] 3.3 In `CreateBulkEmailValidator`: remove the "exactly one of attendee/externalList" custom rule and the empty-external-list rule; keep subject/body/filter validation.
- [x] 3.4 Make `DisplayName` non-nullable in read DTOs `PreviewBulkEmailResponse` and `BulkEmailJobDetailDto` (and their projections in the preview/get handlers).

## 4. Persistence

- [x] 4.1 Update `BulkEmailJobEntityConfiguration`: rename `source` column to `attendee_filter` with a converter for `BulkEmailAttendeeFilter` (jsonb); mark recipients `display_name` and `registration_id` JSON properties `IsRequired()`.
- [x] 4.2 Generate the EF migration via the `ef-migrations` skill/tooling (column rename; no data backfill — table is empty). Do not hand-edit the migration.

## 5. Admin UI

- [x] 5.1 Regenerate the Admin UI SDK per `AGENTS.md` (aspire start/wait, fetch `/openapi/v1.json`, `pnpm openapi-ts`) after the contract change.
- [x] 5.2 Remove CSV logic from `send-bulk-email-sheet.tsx` (file input, `parseCsv`, `CSV_ROW_LIMIT`, `CsvRow`, `handleFileChange`, the attendees-vs-external-list toggle); keep attendee-filter selection only, using generated request types.
- [x] 5.3 Update the campaign detail page to drop the "External list (N recipients)" source descriptor and show the attendee filter only.
- [x] 5.4 Update bulk-email proxy routes if their request/response types changed after SDK regen.

## 6. Tests

- [x] 6.1 Update `BulkEmailRecipientResolverTests`: remove `ExternalListSource` cases; keep/extend attendee resolution and add the filter→contract mapping assertion.
- [x] 6.2 Update `Admitto.Api.Tests/Email/BulkEmail` (`CreateTests`, `PreviewTests`, list/detail): drop the both-sources/external-list cases; assert the attendee-filter contract.
- [x] 6.3 Update the `BulkEmailJobRequestedDomainEventHandlerTests` / `GetBulkEmailsHandlerTests` and any fixtures referencing the old source shape.
- [x] 6.4 Run architecture tests first, then the affected Email domain/integration/API suites; fix failures.

## 7. Docs & spec sync

- [x] 7.1 Update `docs/adrs/adr-009-bulk-email-design.md` to record the single-source model and the Email-owned filter decoupling.
- [x] 7.2 Update `docs/arc42/05-building-block-view.md` and `06-runtime-view.md` bulk-email notes (remove external-list source; note the filter mapping at the facade boundary).
- [x] 7.3 After implementation, sync/archive the change specs (`bulk-email`, `admin-ui-bulk-emails`, `cli-admin-parity`) via the OpenSpec archive workflow.
