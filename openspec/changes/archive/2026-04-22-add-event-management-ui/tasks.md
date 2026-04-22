## 1. Email module scaffold

- [x] 1.1 Create `src/Admitto.Module.Email/` project (Domain/, Application/, Infrastructure/ folders) and `src/Admitto.Module.Email.Contracts/` project; add to `Admitto.slnx`.
- [x] 1.2 Add `EmailModuleKey` constant and `AddEmailModule(...)` DI extension following the pattern of Organization/Registrations.
- [x] 1.3 Wire the Email module into `Admitto.Api`, `Admitto.Worker`, and `Admitto.Migrations` host startups.
- [x] 1.4 Create `EmailDbContext` targeting the `email` schema; register via `AddModuleDatabaseServices<EmailWriteModel, EmailDbContext>()`.
- [x] 1.5 Add `email-db` connection string handling consistent with `admitto-db` (single PostgreSQL instance, schema-per-module per §8.8).

## 2. Email domain and persistence

- [x] 2.1 Add `EventEmailSettings` aggregate (id = `TicketedEventId`) with fields: SmtpHost, SmtpPort, FromAddress, AuthMode (None|Basic), Username, ProtectedPassword, Version. Include `IsValid` domain check.
- [x] 2.2 Add `EventEmailSettings.Errors` nested error class for create/update business rules.
- [x] 2.3 Add EF `IEntityTypeConfiguration<EventEmailSettings>` mapping to `email.event_email_settings`; configure unique index on `TicketedEventId`.
- [x] 2.4 Add `IProtectedSecret` infrastructure adapter wrapping `IDataProtectionProvider` with purpose string `"Admitto.Email.ConnectionString.v1"`; register in DI.
- [x] 2.5 Add EF value converter (or property accessor) that uses `IProtectedSecret` to encrypt on write and decrypt on read for the password column.
- [x] 2.6 Configure shared Data Protection key ring persisted to a stable backing store (DB table or blob), shared across API and Worker hosts.
- [x] 2.7 Generate EF migration for `email.event_email_settings` via the `ef-migrations` skill; verify Migrations host applies it.

## 3. Email module use cases and endpoints

- [x] 3.1 Implement `CreateEventEmailSettingsCommand` + handler (rejects if a row already exists for the event).
- [x] 3.2 Implement `UpdateEventEmailSettingsCommand` + handler with optimistic concurrency on `Version`; preserve stored secret when not supplied.
- [x] 3.3 Implement `GetEventEmailSettingsQuery` + handler returning DTO with masked secret metadata (host, port, fromAddress, authMode, hasPassword, version).
- [x] 3.4 Add `AdminApi/` HTTP request/validator/endpoint for `PUT /admin/teams/{teamSlug}/events/{eventSlug}/email-settings` (creates or updates).
- [x] 3.5 Add `AdminApi/` HTTP endpoint for `GET /admin/teams/{teamSlug}/events/{eventSlug}/email-settings`.
- [x] 3.6 Wire endpoints in the module's endpoint registration entry point with `RequireTeamMembership(role)`.
- [x] 3.7 Implement `IEventEmailFacade` in `Admitto.Module.Email.Contracts` with `IsEmailConfiguredAsync(TicketedEventId, CancellationToken)`.
- [x] 3.8 Add facade implementation in `Admitto.Module.Email`; register in DI for all hosts (no `RequiresCapability`).

## 4. Email module tests

- [x] 4.1 Create `tests/Admitto.Module.Email.Domain.Tests/` covering `EventEmailSettings` create/update and `IsValid` rules.
- [x] 4.2 Create `tests/Admitto.Module.Email.Tests/` integration tests for create/update/get handlers and unique-event constraint.
- [x] 4.3 Add test that confirms password column does not contain plaintext after save (Data Protection at rest).
- [x] 4.4 Add facade tests covering: configured row → true; missing row → false; row missing required fields → false.

## 5. Registrations module — explicit RegistrationStatus

- [x] 5.1 Add `RegistrationStatus { Draft, Open, Closed }` value object/enum in Registrations Domain.
- [x] 5.2 Add `RegistrationStatus` property to `EventRegistrationPolicy` with `OpenForRegistration()` and `CloseForRegistration()` methods; add nested `Errors` for invalid transitions and lifecycle conflicts.
- [x] 5.3 Update self-service and coupon registration handlers to reject when `RegistrationStatus != Open` (in addition to existing window/lifecycle checks); add new handler-local errors.
- [x] 5.4 Generate EF migration adding `registration_status` column with default `Open` for existing rows.
- [x] 5.5 Update domain tests for `EventRegistrationPolicy` covering Draft default, Open/Close transitions, and registration rejection when not `Open`.

## 6. Registrations module — open/close + can-open status

- [x] 6.1 Implement `OpenRegistrationCommand` + handler. Call `IEventEmailFacade.IsEmailConfiguredAsync` before status transition; throw `BusinessRuleViolationException` with handler-local `EmailNotConfigured` error when false. Also enforce lifecycle guard.
- [x] 6.2 Implement `CloseRegistrationCommand` + handler (idempotent on `Closed`).
- [x] 6.3 Implement `GetRegistrationOpenStatusQuery` + handler returning `{ status, canOpen, reason }`; consults Email facade and lifecycle status.
- [x] 6.4 Add `AdminApi/` endpoints: `POST .../registration/open`, `POST .../registration/close`, `GET .../registration/open-status`.
- [x] 6.5 Wire endpoints in Registrations endpoint registration entry point with `RequireTeamMembership(role)`.
- [x] 6.6 Add reference from `Admitto.Module.Registrations` to `Admitto.Module.Email.Contracts` (Contracts only — no implementation reference).

## 7. Registrations module — tests

- [x] 7.1 Integration tests for `OpenRegistration` covering success, EmailNotConfigured rejection (with NSubstitute fake `IEventEmailFacade`), lifecycle Cancelled rejection, and re-opening from Closed.
- [x] 7.2 Integration tests for `CloseRegistration` covering Open→Closed and idempotent Closed→Closed.
- [x] 7.3 Integration tests for `GetRegistrationOpenStatus` covering canOpen=true and canOpen=false (email-not-configured) cases.
- [x] 7.4 Update existing self-service / coupon registration tests so events default to `Open` where needed (or seed via builder).

## 8. Organization → Registrations event-creation sync

- [x] 8.1 Add `TicketedEventCreatedModuleEvent(TeamId, TicketedEventId)` in `Admitto.Module.Organization.Contracts`; publish it from `OrganizationMessagePolicy` based on `TicketedEventCreatedDomainEvent` (alongside the existing Cancelled/Archived publications). Extend `TicketedEventCreatedDomainEvent` with the `TicketedEventId` so the message carries it.
- [x] 8.2 Add Registrations use case `Application/UseCases/EventLifecycleSync/HandleEventCreated/` containing `HandleEventCreatedCommand`, an idempotent `HandleEventCreatedHandler` (no-op when a policy already exists; otherwise creates a fresh `EventRegistrationPolicy` with `EventLifecycleStatus = Active` and `RegistrationStatus = Draft`), and a `TicketedEventCreatedModuleEventHandler` that dispatches the command via the mediator using `DeterministicCommandId`.
- [x] 8.3 Add a single shared `EventRegistrationPolicy.Errors.EventNotFound` (`ErrorType.NotFound`) used by every Registrations handler that requires the policy. Removes the previous "auto-create on demand" branches from `OpenRegistrationHandler`, `CloseRegistrationHandler`, `SetRegistrationPolicyHandler`, `AddTicketTypeHandler`, `UpdateTicketTypeHandler`, `CancelTicketTypeHandler`, `CreateCouponHandler`, `SelfRegisterAttendeeHandler`, `RegisterWithCouponHandler`, and the synthetic-Draft fallback in `GetRegistrationOpenStatusHandler`.
- [x] 8.4 Add integration tests for `HandleEventCreatedHandler`: fresh event creates a Draft + Active policy; re-delivery is a no-op (idempotent). Update `SC005_AddTicketType_NoPolicyExists_*` and `SetRegistrationPolicy` tests to match the new contract (seed a policy, or assert `EventNotFound`).
- [x] 8.5 Add/extend admin endpoint for `PUT /admin/teams/{teamSlug}/events/{eventSlug}` to support General-tab editable fields (name, start, end) with optimistic concurrency, if not already complete.
- [x] 8.6 Add tests for any new/updated Organization endpoints.

## 9. CLI parity

- [x] 9.1 Add CLI commands for: get/update event general settings (Organization). _(Already covered by existing `event show` / `event update` commands, which support name/website/baseUrl/start/end with `--expected-version` for optimistic concurrency.)_
- [ ] 9.2 Add CLI commands for: open/close registration, get open-status (Registrations). _**Blocked by 9.4.**_
- [ ] 9.3 Add CLI commands for: get/update event email settings (Email). _**Blocked by 9.4.**_
- [ ] 9.4 Regenerate `Admitto.Cli/Api/ApiClient.g.cs` via the `cli-api-client-generation` skill. _**Blocked.** Regeneration succeeds against the live API and produces correct methods for the new endpoints, but the current API surface no longer exposes `AttendeeDto`, `AdditionalDetailSchemaDto`, or the legacy `TeamMemberRole` type. Several pre-existing CLI commands (`Commands/Attendee/ExportAttendeesCommand.cs`, `Commands/Events/CreateEventCommand.cs`, `Commands/Team/Member/*`, `Api/ApiClientMemberExtensions.cs`) reference these removed types and only compiled because the committed `ApiClient.g.cs` is stale. Regenerating exposes that drift and breaks the CLI build. Cleaning up the broken legacy commands is a separate, sizeable cleanup outside this change's scope; track in a follow-up change before attempting 9.2–9.4 again._

## 10. Admin UI — routing and create event

- [x] 10.1 Add route `app/(dashboard)/teams/[teamSlug]/events/new/page.tsx` with `useCustomForm` + Zod schema for slug/name/start/end.
- [x] 10.2 Wire "Create Event" button on the team's events list page to the new route.
- [x] 10.3 Regenerate the HeyAPI TypeScript SDK to pick up new endpoints (Organization/Registrations/Email).

## 11. Admin UI — tabbed event settings layout

- [x] 11.1 Create `app/(dashboard)/teams/[teamSlug]/events/[eventSlug]/settings/layout.tsx` mirroring the team settings side-nav with tabs General / Registration / Email.
- [x] 11.2 Create `settings/page.tsx` (General tab) with form for name/start/end, optimistic concurrency via `Version`, and ProblemDetails error mapping.
- [x] 11.3 Update event navigation in the dashboard to surface "Settings" alongside existing event pages.

## 12. Admin UI — Registration tab

- [x] 12.1 Create `settings/registration/page.tsx` showing window, allowed-email-domain, ticket-types list, and current `RegistrationStatus`.
- [x] 12.2 Add "Open for registration" / "Close for registration" actions; query the `open-status` endpoint and disable "Open" with linked hint when `canOpen=false` (`reason: email-not-configured`).
- [x] 12.3 Surface backend ProblemDetails on action failure (e.g. race where email becomes unconfigured between status query and action).
- [x] 12.4 Add ticket-type create/edit/delete UI (independent forms, each with its own concurrency token).

## 13. Admin UI — Email tab

- [x] 13.1 Create `settings/email/page.tsx` with form for SMTP host/port/from-address/auth mode/username/password.
- [x] 13.2 Render password as masked + empty on load; include `hasPassword` indicator returned by GET endpoint.
- [x] 13.3 Submit only changed secret fields; ensure unchanged password is omitted from the request payload (so backend preserves the stored value).
- [x] 13.4 Display "Email is configured" / "Email not configured" status banner driven by GET response.

## 14. Documentation

- [x] 14.1 Update `docs/arc42/05-building-block-view.md` — add the Email module to the modules diagram and table; note its `Contracts` exposes `IEventEmailFacade`.
- [x] 14.2 Update `docs/arc42/08-crosscutting-concepts.md` (§8.4 or new subsection) noting the synchronous cross-module facade pattern is also used by Registrations→Email.
- [x] 14.3 Note ASP.NET Data Protection use for email secrets in `docs/arc42/08-crosscutting-concepts.md` under a "Secret protection" subsection or in the Email module description.

## 15. Verification

- [x] 15.1 Run domain tests for Registrations and Email modules; all pass.
- [x] 15.2 Run module tests for Registrations and Email; all pass.
- [x] 15.3 Run `Admitto.Api.Tests`; all pass.
- [x] 15.4 Manual smoke via Aspire AppHost: create event → confirm Draft → attempt open without email → expect rejection → configure email → open succeeds → self-register → close → self-register rejected.
- [x] 15.5 Run `openspec validate add-event-management-ui` and resolve any issues.

## 16. Email module — Value Object refactor

- [x] 16.1 Add `Hostname` VO under `src/Admitto.Module.Email/Domain/ValueObjects/` — `IStringValueObject`, permissive (non-empty + `MaxLength = 253`), via `StringValueObject.TryFrom`.
- [x] 16.2 Add `Port` VO under `src/Admitto.Module.Email/Domain/ValueObjects/` — `IInt32ValueObject`, range 1–65535, via `Int32ValueObject.TryFrom`.
- [x] 16.3 Add `SmtpUsername` VO under `src/Admitto.Module.Email/Domain/ValueObjects/` — `IStringValueObject`, `MaxLength = 256`.
- [x] 16.4 Add `ProtectedPassword` marker VO under `src/Admitto.Module.Email/Domain/ValueObjects/` — opaque `record struct ProtectedPassword`; `From(string ciphertext)` factory only, no format check.
- [x] 16.5 Update `EventEmailSettings` aggregate: change ctor / `Create` / `Update` / property types to use the new VOs; shrink `EnsureValidShape` to the Basic-auth cross-field rule only; drop now-redundant `Errors.SmtpHostRequired` and `Errors.SmtpPortInvalid`.
- [x] 16.6 Update `EventEmailSettingsEntityConfiguration`: use `Foo.MaxLength` instead of literals; add EF value converters for `Hostname`, `Port`, `SmtpUsername`, `ProtectedPassword`.
- [x] 16.7 Update `CreateEventEmailSettingsCommand` / `UpdateEventEmailSettingsCommand` and handlers to convert primitives → VOs at the handler boundary via `Foo.From(...)` (input is pre-validated by FluentValidation).
- [x] 16.8 Replace ad-hoc rules in `UpsertEventEmailSettingsValidator` with `MustBeParseable(Foo.TryFrom)` for hostname / port / username; keep cross-field Basic-auth rule.
- [x] 16.9 Verify EF migration: if `email.event_email_settings` column lengths/types change vs. snapshot, regenerate the migration via the `ef-migrations` skill; otherwise note no migration needed.
- [x] 16.10 Add VO unit tests under `tests/Admitto.Module.Email.Domain.Tests/Domain/ValueObjects/` — `TryFrom` happy path + each error branch, `From` throw-on-error, and `ProtectedPassword` round-trip.
- [x] 16.11 Update `EventEmailSettingsBuilder` + existing aggregate tests + integration fixtures to construct via VOs instead of primitives.
- [x] 16.12 Add new section `8.X Value objects` to `docs/arc42/08-crosscutting-concepts.md` covering: anatomy, `MaxLength` constant on the VO, `MustBeParseable(Foo.TryFrom)` validator integration, EF value converters, marker types, module-local-vs-shared-kernel placement, what does NOT belong in a VO.
- [x] 16.13 Cross-link the new VO section from `docs/arc42/05-building-block-view.md` (Domain folder description) and from `§8.7` (errors) / `§8.8` (persistence).
- [x] 16.14 Run `dotnet build Admitto.slnx`; run `Admitto.Module.Email.Domain.Tests` and `Admitto.Module.Email.Tests` — all green.
