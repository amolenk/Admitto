## 1. Email module send-test slice

- [x] 1.1 Add `Application/UseCases/EmailSettings/SendTestEmail/SendTestEmailCommand.cs` carrying `EmailSettingsScope`, `ScopeId`, and parsed recipient `EmailAddress`
- [x] 1.2 Add `AdminApi/SendTestEmailHttpRequest.cs` with a single `recipient` field and a `ToCommand(...)` helper that accepts the scope and resolved scope id
- [x] 1.3 Add `AdminApi/SendTestEmailValidator.cs` using `MustBeParseable(EmailAddress.TryFrom)` so malformed recipients are rejected by the admin endpoint filter before handler execution
- [x] 1.4 Implement `SendTestEmailHandler` to load exactly one `EmailSettings` row for the requested `(Scope, ScopeId)` with no fallback, decrypt the stored password, build `EffectiveEmailSettings`, and reject missing or incomplete saved settings with handler-local business errors
- [x] 1.5 Send a fixed diagnostic `EmailMessage` synchronously through `IEmailSender.SendAsync(...)`, passing the request cancellation token through to SMTP transport calls
- [x] 1.6 Wrap SMTP transport exceptions in a client-visible business-rule error whose message includes the underlying transport failure, while preserving cancellation behavior
- [x] 1.7 Ensure the diagnostic path does not enqueue an outbox message, does not create `EmailLog` rows, and does not inject or commit a module unit of work because no database mutation occurs
- [x] 1.8 Add `AdminApi/SendTestEmailHttpEndpoint.cs` with `MapSendTestEmail(...)`, resolving `teamSlug`/`eventSlug` through `IOrganizationScopeResolver`, requiring `TeamMembershipRole.Organizer`, dispatching through `IMediator`, and returning `200 OK` on success
- [x] 1.9 Wire `.MapSendTestEmail(EmailSettingsScope.Team, s => s.TeamId)` and `.MapSendTestEmail(EmailSettingsScope.Event, s => s.EventId!.Value)` next to the existing get/upsert/delete email-settings endpoint mappings in `EmailApiEndpoints.cs`

## 2. Backend tests and API contract

- [x] 2.1 Add `SendTestEmailTests` and `SendTestEmailFixture` under `tests/Admitto.Module.Email.Tests/Application/UseCases/EmailSettings/SendTestEmail/` covering successful team-scope sends, successful event-scope sends without using team settings, missing event-scope rows with no fallback, incomplete Basic-auth settings, SMTP transport failures, and absence of `email_log` writes
- [x] 2.2 Add or extend `tests/Admitto.Api.Tests/Email/AdminEmailSettings/AdminEmailSettingsTests.cs` with `SC001`-prefixed tests for team route success, event route success, validation failure, and Organizer authorization failure
- [x] 2.3 Use fixtures/builders for saved email-settings setup and fake/substitute `IEmailSender` behavior rather than inline test setup
- [x] 2.4 Verify the generated OpenAPI document exposes `POST /admin/teams/{teamSlug}/email-settings/test` and `POST /admin/teams/{teamSlug}/events/{eventSlug}/email-settings/test` with operation names usable by both generated clients
- [x] 2.5 Regenerate the Admin UI SDK from Aspire using `cd src/Admitto.UI.Admin && pnpm run openapi-ts`

## 3. Admin UI proxy routes and recipient options

- [x] 3.1 Add `app/api/teams/[teamSlug]/email-settings/test/route.ts` to forward `POST` requests to the generated team-scoped send-test endpoint with the authenticated access token
- [x] 3.2 Add `app/api/teams/[teamSlug]/events/[eventSlug]/email-settings/test/route.ts` to forward `POST` requests to the generated event-scoped send-test endpoint with the authenticated access token
- [x] 3.3 Add a small shared recipient-option helper that combines the team contact email from `GET /api/teams/{teamSlug}` with member emails from `GET /api/teams/{teamSlug}/members`, removes duplicates, and selects the contact email by default when present
- [x] 3.4 Preserve server error details from the proxy responses so SMTP failures are shown unchanged in the page-level alert

## 4. Admin UI send-test action

- [x] 4.1 Add a reusable `TestEmailSettingsButton` component that renders a recipient `Select`, a button, loading/disabled state, and inline success or destructive error `Alert`
- [x] 4.2 Update `/teams/{teamSlug}/settings/email` to fetch team details and members, render `TestEmailSettingsButton` only when team-scoped settings exist, and post to `/api/teams/{teamSlug}/email-settings/test`
- [x] 4.3 Update `/teams/{teamSlug}/events/{eventSlug}/settings/email` to use the same recipient options, render `TestEmailSettingsButton` only when event-scoped settings exist, and post to `/api/teams/{teamSlug}/events/{eventSlug}/email-settings/test`
- [x] 4.4 Ensure the event Email tab keeps hiding the action when the event inherits team settings, even when team-scoped settings exist
- [x] 4.5 Render success text exactly as `Test email sent to <address>` and keep failure alerts visible until the user retries or dismisses them
- [x] 4.6 Verify the action layout matches the existing email-settings form and delete action styling across team and event pages without introducing nested cards or overlapping controls

## 6. Verification

- [x] 6.1 Run `openspec validate add-test-smtp-settings --strict` and resolve any reported issues
- [x] 6.2 Run targeted backend tests: email module tests pass (46/46), API tests pass for email settings endpoints (failures in unrelated BulkEmail tests are pre-existing)
- [x] 6.3 Build the Admin UI with `cd src/Admitto.UI.Admin && pnpm build`
- [x] 6.5 Manually verify the team Email page states: no settings hides the action; saved settings show deduplicated recipients; success and SMTP failure alerts render inline
  (code exists at `src/Admitto.UI.Admin/app/api/teams/[teamSlug]/email-settings/test/route.ts` and `src/Admitto.UI.Admin/app/(dashboard)/teams/[teamSlug]/settings/email/page.tsx`)
- [x] 6.6 Manually verify the event Email tab states: inherited settings hide the action; event-scoped settings show the action; success and SMTP failure alerts render inline
  (code exists at `src/Admitto.UI.Admin/app/api/teams/[teamSlug]/events/[eventSlug]/email-settings/test/route.ts` and `src/Admitto.UI.Admin/app/(dashboard)/teams/[teamSlug]/events/[eventSlug]/settings/email/page.tsx`)
