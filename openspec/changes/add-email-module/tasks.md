## 1. Registrations: publish AttendeeRegisteredIntegrationEvent

- [x] 1.1 Add `AttendeeRegisteredIntegrationEvent` record in `src/Admitto.Module.Registrations.Contracts/IntegrationEvents/` with fields `TeamId`, `TicketedEventId`, `RegistrationId`, `RecipientEmail`, `RecipientName` (inherits `IntegrationEvent`).
- [x] 1.2 Wire mapping `AttendeeRegisteredDomainEvent` → `AttendeeRegisteredIntegrationEvent` in `RegistrationsMessagePolicy` (`PublishIntegrationEvent`).
- [x] 1.3 Add a domain-tests-level test verifying the policy publishes the integration event for the domain event.
- [x] 1.4 Add an integration test in `tests/Admitto.Module.Registrations.Tests` that registers an attendee and asserts the integration event is appended to the outbox.

## 2. Email module: domain & infrastructure for templates, settings, and log

- [x] 2.1 Add `EmailTemplate` aggregate in `src/Admitto.Module.Email/Domain/Entities/` with fields `Scope` (enum: Team, Event), `ScopeId` (Guid), `Type` (string), `Subject`, `TextBody`, `HtmlBody`, plus `Version`.
- [x] 2.2 Add `EmailTemplateType` constants class with `Ticket = "ticket"` (extension point for future types).
- [x] 2.3 Add unified `EmailSettings` aggregate in `Domain/Entities/` keyed by `(Scope, ScopeId)` where `Scope ∈ {Team, Event}` and `ScopeId` is a `Guid`. Carry the same fields and `IsValid()` semantics as the previous `EventEmailSettings`. Remove `EventEmailSettings`.
- [x] 2.4 Add `EmailLog` entity in `Domain/Entities/` with fields per spec (`Id`, `TeamId`, `TicketedEventId`, `IdempotencyKey`, `Recipient`, `EmailType`, `Subject`, `Provider`, `ProviderMessageId?`, `Status`, `SentAt?`, `StatusUpdatedAt`, `LastError?`).
- [x] 2.5 Add EF entity configurations for `EmailTemplate`, `EmailSettings`, `EmailLog`. Settings: map to table `email.email_settings`, columns `scope` + `scope_id`, unique index on `(scope, scope_id)`. Replace the existing `EventEmailSettingsEntityConfiguration` with `EmailSettingsEntityConfiguration`. Email log: unique index on `(ticketed_event_id, recipient, idempotency_key)`; secondary index on `(ticketed_event_id, sent_at DESC)`.
- [x] 2.6 Extend `IEmailWriteStore` and `EmailDbContext` with `DbSet`s for `EmailTemplate`, `EmailSettings`, `EmailLog`; remove the old `EventEmailSettings` `DbSet`.
- [x] 2.7 Generate a single EF migration in `Admitto.Module.Email/Infrastructure/Persistence/Migrations/` via the official tooling (skill: `ef-migrations`); do NOT hand-edit migration files. The migration MUST be data-preserving for any pre-existing rows in `event_email_settings`: rename the table to `email_settings`, add `scope` (defaulted to `'event'`) and `scope_id` (populated from the existing `ticketed_event_id`) columns, drop the legacy primary key, add a surrogate `id` PK, and add the unique index on `(scope, scope_id)`. Verify by inspection that the generated migration performs these data-preserving steps.
- [x] 2.8 Add domain-tests-level tests for `EmailTemplate.Create/Update`, `EmailSettings.Create/Update/IsValid` covering both `Scope=Team` and `Scope=Event`, and basic `EmailLog` invariants.

## 3. Email module: settings resolution

- [x] 3.1 Add internal record `EffectiveEmailSettings` in `Application/Settings/` carrying decrypted host/port/from/auth/credentials.
- [x] 3.2 Add internal contract `IEffectiveEmailSettingsResolver` (Email-module-internal) with method `ValueTask<EffectiveEmailSettings?> ResolveAsync(TicketedEventId eventId, CancellationToken ct)`.
- [x] 3.3 Implement the resolver: query `EmailSettings` for `(Scope=Event, ScopeId=eventId)`; if absent, look up the owning team via `IOrganizationFacade` and query `EmailSettings` for `(Scope=Team, ScopeId=teamId)`; return null if neither. Decrypt password via `IProtectedSecret` inside the resolver. Both queries hit the single `email.email_settings` table.
- [x] 3.4 Update `EventEmailFacade.IsEmailConfiguredAsync` to use the resolver and return `effective is not null && effective.IsValid()`.
- [x] 3.5 Add unit tests covering: event-scope-only, team-scope-only, both-present (event wins), neither (null), event-invalid + team-valid (returns false because event wins).

## 4. Email module: template service

- [x] 4.1 Add `IEmailTemplateService` in `Application/Templating/` with `LoadAsync(string type, TeamId teamId, TicketedEventId eventId, CancellationToken ct)` returning `EmailTemplate` (DB row or default).
- [x] 4.2 Implement template lookup precedence (event-scoped → team-scoped → built-in default) in a single EF query plus default-fallback.
- [x] 4.3 Add `IEmailRenderer` (or method on the template service) using Scriban to render subject/text/html from a parameters object; surface parse errors as a deterministic `EmailRenderException`.
- [x] 4.4 Add embedded resources `Application/Templating/Defaults/ticket.html` and `ticket.txt` (port from `Admitto.bak/src/Admitto.Application/Common/Email/Templating/Defaults/ticket.{html,txt}`); register them as `<EmbeddedResource>` in the csproj.
- [x] 4.5 Add unit tests covering all three precedence outcomes, an unknown type, and a render-error path.

## 5. Email module: send command + SMTP adapter

- [x] 5.1 Add `SendEmailCommand` (`TeamId`, `TicketedEventId`, `Recipient`, `EmailType`, `IdempotencyKey`, `Parameters` payload) in `Application/UseCases/SendEmail/`.
- [x] 5.2 Add `SendEmailCommandHandler` annotated `[RequiresCapability(HostCapability.Email)]`. Flow: pre-check email log for the dedup key (skip if present); resolve effective settings (write `Failed` log + return if null); resolve + render template (catch `EmailRenderException` → write `Failed` + return); call `IEmailSender.SendAsync`; on success append `Sent` row; on SMTP exception append `Failed` row and rethrow only if transient. Catch unique-violation as "already sent".
- [x] 5.3 Add `IEmailSender` abstraction in `Application/Sending/` with `ValueTask SendAsync(EffectiveEmailSettings settings, EmailMessage message, CancellationToken ct)` and a friendly `Provider` name.
- [x] 5.4 Implement `MailKitEmailSender` in `Infrastructure/Sending/` opening a per-call `SmtpClient` and respecting auth mode. Add MailKit NuGet package via `Directory.Packages.props`.
- [x] 5.5 Register `IEmailSender` and the MailKit adapter behind `HostCapability.Email` in `Email/Infrastructure/DependencyInjection.cs`.
- [x] 5.6 Add integration tests using a fake `IEmailSender` to verify: success path writes `Sent` row; missing settings writes `Failed`; render error writes `Failed`; duplicate command (same idempotency key) does not double-send.

## 6. Email module: integration-event handler

- [x] 6.1 Add `AttendeeRegisteredIntegrationEventHandler` (no capability gate) in `Application/UseCases/SendEmail/EventHandlers/`. Behavior: build idempotency key `attendee-registered:{registrationId}`; pre-check log; if not present, dispatch a `SendEmailCommand` (via in-process mediator pipeline) so the same idempotency + dedup logic runs in stage two.
- [x] 6.2 Confirm `AddIntegrationEventHandlersFromAssembly` (or equivalent) discovers and registers it in any host that processes the Registrations queue (API host).
- [x] 6.3 Add an end-to-end test in `tests/Admitto.Api.Tests` that registers an attendee, drains the queue, and asserts exactly one email was "sent" through the fake sender and one row exists in the email log; redelivering the integration event SHALL NOT double-send.

## 7. Email module: admin endpoints (CRUD)

- [x] 7.1 Add a single `EmailSettings` admin slice family parameterised by `(Scope, ScopeId)` with handlers for `Get`, `Create`, `Update`, `Delete` (mirror the existing `EmailTemplate` admin pattern). The same handler types serve both scopes.
- [x] 7.2 Wire team-scoped settings endpoints: `GET/POST/PUT/DELETE /admin/teams/{teamSlug}/email-settings`. Resolve `(Scope=Team, ScopeId=teamId)` from `{teamSlug}`. Authorize via team membership on `{teamSlug}`. Replace the previous event-scoped-only registration so both routes call the unified slice.
- [x] 7.3 Wire event-scoped settings endpoints: `GET/POST/PUT/DELETE /admin/teams/{teamSlug}/events/{eventSlug}/email-settings`. Resolve `(Scope=Event, ScopeId=eventId)` from `{eventSlug}`. Authorize via team membership on `{teamSlug}`.
- [x] 7.4 Add admin endpoints for templates at team scope: `GET /admin/teams/{teamSlug}/email-templates/{type}`, `PUT` (upsert), `DELETE`.
- [x] 7.5 Add admin endpoints for templates at event scope: `GET /admin/teams/{teamSlug}/events/{eventSlug}/email-templates/{type}`, `PUT` (upsert), `DELETE`.
- [x] 7.6 Add FluentValidation validators for each admin request DTO; wire endpoints in the module's endpoint registration entry point.
- [x] 7.7 Add CLI commands in `src/Admitto.Cli/Commands/` for each new admin endpoint at both scopes (per `src/Admitto.Cli/AGENTS.md`).
- [x] 7.8 Add API endpoint tests for: settings CRUD at team scope, settings CRUD at event scope (must hit the same handler types), team template CRUD, event template CRUD, including 403 for non-team-members and 409 on stale `Version`.

## 8. Documentation & wiring

- [x] 8.1 Update `docs/arc42/05-building-block-view.md` Email-module description to note: actual sending, templates, log, and unified team/event-scoped settings (single `EmailSettings` aggregate keyed by `(Scope, ScopeId)`).
- [x] 8.2 Add a registration-confirmation runtime flow to `docs/arc42/06-runtime-view.md` (sequence: register → outbox → integration event → email handler → SendEmail command on email outbox → worker SMTP send → log).
- [x] 8.3 Confirm `Admitto.Worker/Program.cs` declares `HostCapability.Email`; if anything is missing (e.g. resolver wiring), add it.
- [x] 8.4 Update `aspire.config.json` / Aspire AppHost only if a MailDev resource is not already wired; verify local-dev SMTP works against MailDev.
- [x] 8.5 Run targeted tests:
  - `dotnet test tests/Admitto.Module.Email.Domain.Tests`
  - `dotnet test tests/Admitto.Module.Email.Tests`
  - `dotnet test tests/Admitto.Module.Registrations.Domain.Tests`
  - `dotnet test tests/Admitto.Module.Registrations.Tests`
  - `dotnet test tests/Admitto.Api.Tests`

## 9. Verification

- [x] 9.1 `openspec validate add-email-module --strict` passes.
- [ ] 9.2 Manual smoke: run AppHost locally, register an attendee against MailDev, observe the rendered email, the `email_log` row, and that re-publishing the queue message does NOT produce a second email.
- [ ] 9.3 Manual rollback rehearsal: unregister the email integration-event handler in DI; verify the integration event is acked as no-op and registration still succeeds.
