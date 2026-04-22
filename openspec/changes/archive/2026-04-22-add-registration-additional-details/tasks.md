## 1. Domain — Registrations module

- [x] 1.1 Add `AdditionalDetailField` value object (`Key`, `Name`, `MaxLength`) under `Domain/ValueObjects/`, with validation: key matches `^[a-z0-9][a-z0-9-]{0,49}$`, name 1–100 chars, maxLength 1–4000.
- [x] 1.2 Add `AdditionalDetailSchema` value object wrapping an ordered `IReadOnlyList<AdditionalDetailField>`, validating uniqueness of `Key` and case-insensitive uniqueness of `Name`, and enforcing the 25-field cap.
- [x] 1.3 Extend `TicketedEvent` with the `AdditionalDetailSchema` (default empty) and an `UpdateAdditionalDetailSchema(IReadOnlyList<AdditionalDetailField>)` method that rejects when `Status` is Cancelled or Archived.
- [x] 1.4 Add `AdditionalDetailSchemaUpdatedDomainEvent`; raise it from the aggregate method.
- [x] 1.5 Add `AdditionalDetails` value object on `Registration` (immutable `IReadOnlyDictionary<string, string>`), and a `Validate(AdditionalDetailSchema)` helper used by registration command handlers.
- [x] 1.6 Domain unit tests for value objects and `TicketedEvent.UpdateAdditionalDetailSchema` (success + every rejection scenario in the event-management delta spec).

## 2. Persistence — Registrations module

- [x] 2.1 Map `TicketedEvent.AdditionalDetailSchema` (e.g., as a `jsonb` column on the `ticketed_events` table) via EF entity configuration.
- [x] 2.2 Map `Registration.AdditionalDetails` as a `jsonb` column on the `registrations` table (round-trip via `IReadOnlyDictionary<string, string>`).
- [x] 2.3 Generate the EF Core migration (`AddAdditionalDetails`) using the `ef-migrations` skill (`dotnet ef migrations add AddAdditionalDetails --project src/Admitto.Module.Registrations --startup-project src/Admitto.Migrations --context RegistrationsDbContext --output-dir Infrastructure/Persistence/Migrations`), verify the generated SQL, and ensure the model snapshot matches.

## 3. Application — admin command and handler

- [x] 3.1 Add `UpdateAdditionalDetailSchemaCommand` (TeamSlug, EventSlug, Version, fields[]) and `UpdateAdditionalDetailSchemaHandler` under `Application/UseCases/TicketedEvents/UpdateAdditionalDetailSchema/`.
- [x] 3.2 Add FluentValidation `UpdateAdditionalDetailSchemaRequestValidator` that mirrors the domain rules and the "no duplicates / 25-field cap" constraints.
- [x] 3.3 Add the admin endpoint `PUT /admin/teams/{teamSlug}/events/{eventSlug}/additional-detail-schema` (request, response, endpoint registration).
- [x] 3.4 Integration tests covering success, concurrency conflict, validation failures, and event-status rejection.

## 4. Application — registration handlers

- [x] 4.1 Extend `SelfRegisterAttendeeCommand` and request DTO with `AdditionalDetails` (`Dictionary<string, string>?`).
- [x] 4.2 In `SelfRegisterAttendeeHandler`, read the event's current `AdditionalDetailSchema` and call `AdditionalDetails.Validate(schema)`; reject with `AdditionalDetailKeyNotInSchema` / `AdditionalDetailValueTooLong` as appropriate; persist accepted values on the new `Registration`.
- [x] 4.3 Mirror the same changes in `RegisterWithCouponCommand`/`Handler` (no coupon-bypass for additional details).
- [x] 4.4 Update both public endpoints' request shapes and FluentValidation validators.
- [ ] 4.5 Integration tests for each scenario in the attendee-registration delta spec (success, omitted, partial, empty-string, unknown key, value too long; for both self and coupon paths).

## 5. Application — admin / public read models

> **Deferred to follow-up change.** No `GetRegistrationDetails` admin query and no public `GetEventInfo` endpoint exist today; both 5.1 and 5.2 (and the related Phase 7 display wiring) presume these surfaces. Splitting this work keeps the present change scoped to the schema model, write-side handlers, and UI editor. The follow-up change will own the new read endpoints end-to-end and layer the `additionalDetails` / `historicalAdditionalDetails` payloads.

- [ ] 5.1 *(deferred)* Extend the admin registration detail query/response with `additionalDetails` and `historicalAdditionalDetails`.
- [ ] 5.2 *(deferred)* Extend the public event-info / availability response with the `additionalDetails` array.
- [ ] 5.3 *(deferred)* Tests covering both response shapes, including the historical-vs-current split.

## 6. Admin UI — registration policy page

- [x] 6.1 Add an `AdditionalDetailsEditor` component under the registration policy form: list of rows (Name input, read-only Key, MaxLength input, drag-handle, remove button), "Add field" button, auto-key generation (kebab-case from name) for new rows.
- [x] 6.2 Wire the editor into the existing registration policy form's submit so the schema is sent together with the policy `Version`. Surface validation errors per row.
- [x] 6.3 Show a confirmation dialog when removing a field, with copy explaining historical values are preserved.
- [x] 6.4 Disable the editor and show the "event not active" banner when `TicketedEvent.Status` is Cancelled or Archived (matching the existing policy form behaviour).
- [x] 6.5 Surface concurrency-conflict errors with the standard "reload the page" message.

## 7. Admin UI — registration / attendee detail view

> **Deferred** along with Phase 5 — both rows depend on the deferred admin read model.

- [ ] 7.1 *(deferred)* Render `additionalDetails` (current) as labelled name/value pairs in display order.
- [ ] 7.2 *(deferred)* Render `historicalAdditionalDetails` in a separate "Historical values" section, marked as orphaned, with the bare `key`.

## 8. CLI parity

> **Deferred** — depends on regenerating `src/Admitto.Cli/Api/ApiClient.g.cs` via the `cli-api-client-generation` skill (requires running Aspire AppHost). Tracked as a follow-up; backend admin endpoint is in place and ready for the regeneration.

- [ ] 8.1 *(deferred)* Add `admitto event additional-details list` command (Team, Event) that fetches and prints the current schema.
- [ ] 8.2 *(deferred)* Add `admitto event additional-details set` command (Team, Event, --from-json) that submits an atomic schema replacement with the current `Version`.
- [ ] 8.3 *(deferred)* Note in the change record that delivery of these commands depends on the existing ApiClient regeneration follow-up; if that work is unblocked, regenerate `Admitto.Cli/Api/ApiClient.g.cs` via the `cli-api-client-generation` skill.

## 9. Public website integration

> No existing public event-info contract surface exposes additional details — the contracts project only defines integration events and `RegisterAttendeeResult`. The public registration HTTP request now accepts `additionalDetails` (Phase 4) and the public event-info read path is part of the deferred Phase 5/7 follow-up. No Contracts-project changes are required for this change.

- [x] 9.1 Verify that the existing public event-info contract surface is the right place for the new `additionalDetails` array and document it in the contract README/comments if any.
- [ ] 9.2 *(optional, skipped)* Provide a sample payload in the contracts project's tests so consumers can lock to the new shape.

## 10. Verification

- [x] 10.1 Run module test suites: `dotnet test tests/Admitto.Module.Registrations.Domain.Tests`, `dotnet test tests/Admitto.Module.Registrations.Tests`, `dotnet test tests/Admitto.Api.Tests`.
- [x] 10.2 Run `dotnet build Admitto.slnx` clean.
- [x] 10.3 Run `pnpm build` in `src/Admitto.UI.Admin` clean.
- [x] 10.4 Run `openspec validate add-registration-additional-details --strict` and resolve any findings.
- [ ] 10.5 *(manual)* Manually smoke through: create event → add two additional-detail fields → public self-register supplying values → admin views show values → admin removes one field → admin views show preserved historical value → public registration with the removed key is rejected.
