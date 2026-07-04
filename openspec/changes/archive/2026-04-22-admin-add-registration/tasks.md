## 1. Backend — Application use case

- [x] 1.1 Create `src/Admitto.Module.Registrations/Application/UseCases/Registrations/AdminRegisterAttendee/` folder.
- [x] 1.2 Add `AdminRegisterAttendeeCommand` (record with `TicketedEventId EventId`, `EmailAddress Email`, `string[] TicketTypeSlugs`, `IReadOnlyDictionary<string,string>? AdditionalDetails`, returning `RegistrationId`).
- [x] 1.3 Add `AdminRegisterAttendeeHandler` implementing `ICommandHandler<AdminRegisterAttendeeCommand, RegistrationId>`, following the order in design D3 (load event → active gate → load catalog → reuse `SelfRegisterAttendeeHandler.ValidateTicketTypeSelection` → build snapshots → `catalog.Claim(slugs, enforce: false)` mapping `EventNotActive` → `AdditionalDetails.Validate` → `Registration.Create` → add to write store).
- [x] 1.4 Define handler-local `Errors` (or reuse the existing ones from `SelfRegisterAttendeeHandler.Errors` / `RegisterWithCouponHandler.Errors`) for: `EventNotFound`, `EventNotActive`, `NoTicketTypesConfigured`, `DuplicateTicketTypes`, `UnknownTicketTypes`, `CancelledTicketTypes`, `OverlappingTimeSlots`. Match the existing error codes/messages so client behaviour is consistent across paths.

## 2. Backend — Admin HTTP endpoint

- [x] 2.1 Add `AdminApi/AdminRegisterAttendeeHttpRequest` with `Email`, `TicketTypeSlugs`, `AdditionalDetails`.
- [x] 2.2 Add `AdminApi/AdminRegisterAttendeeHttpResponse` with the new registration id.
- [x] 2.3 Add `AdminApi/AdminRegisterAttendeeValidator` (FluentValidation) — non-empty/well-formed email, at least one ticket-type slug, no empty slugs.
- [x] 2.4 Add `AdminApi/AdminRegisterAttendeeHttpEndpoint` mapping `POST /admin/teams/{teamSlug}/events/{eventSlug}/registrations`, resolving the team+event to a `TicketedEventId`, dispatching the command, owning the transaction (per repo conventions), and returning `201 Created` with the response body.
- [x] 2.5 Register the endpoint in the module's admin endpoint wiring (next to existing admin endpoints in this module).
- [x] 2.6 Confirm the endpoint is covered by the existing team-admin authorisation policy used by sibling admin endpoints.

## 3. Backend — Tests

- [x] 3.1 Add application-level tests under `tests/Admitto.Module.Registrations.Tests/Application/UseCases/Registrations/AdminRegisterAttendee/` with a fixture/builder mirroring existing patterns (`SelfRegisterAttendeeFixture` style).
- [x] 3.2 Implement one test per acceptance scenario in `specs/admin-registration/spec.md` for the "Admin can directly add a registration to a ticketed event" requirement, named with the `SC###_` prefix convention.
- [x] 3.3 Cover the duplicate-email case via the `DbUpdateException` + `PostgresException.ConstraintName` pattern used by `SelfRegisterAttendeeTests`.
- [x] 3.4 Add API endpoint tests under `tests/Admitto.Api.Tests/...` for the "Admin-add registration is exposed via an admin HTTP endpoint" requirement (auth, request validation, success path).

## 4. CLI command

- [x] 4.1 Run `src/Admitto.Cli/generate-api-client.sh` to regenerate `src/Admitto.Cli/Api/ApiClient.g.cs` against the API exposing the new endpoint.
- [x] 4.2 Add a CLI command under `src/Admitto.Cli/Commands/...` (placed in the existing tree to match parity conventions, e.g. an `event registration add` branch) that calls the regenerated `ApiClient` method.
- [x] 4.3 Wire the new command into `Program.cs` under the matching feature branch.
- [ ] 4.4 Verify the command via the scenario in the `admin-registration` spec ("Operator adds a registration from the CLI").

## 5. Admin UI

- [x] 5.1 Add an "Add registration" affordance on the event registrations page (dialog or page per design D6); reuse existing form primitives for consistency with other admin features.
- [x] 5.2 Build the form: email field, ticket-type multi-select sourced from the event's current ticket catalog (excluding cancelled types), and additional-details fields rendered from the event's `AdditionalDetailSchema` (reuse the public registration form's renderer where possible).
- [x] 5.3 Submit via `apiClient` (per repo convention) to the new admin endpoint; on success close the form, refresh the registrations list, and surface a confirmation.
- [x] 5.4 Render server-side validation errors inline (duplicate email on the email field; "event not active" and other generic errors at the form level).
- [x] 5.5 Build the Admin UI with `cd src/Admitto.UI.Admin && pnpm build` to confirm no TypeScript errors.

## 6. Verification

- [x] 6.1 Run `dotnet test tests/Admitto.Module.Registrations.Tests/Admitto.Module.Registrations.Tests.csproj`.
- [x] 6.2 Run `dotnet test tests/Admitto.Module.Registrations.Domain.Tests/Admitto.Module.Registrations.Domain.Tests.csproj`.
- [x] 6.3 Run `dotnet test tests/Admitto.Api.Tests/Admitto.Api.Tests.csproj`.
- [ ] 6.4 Manually verify the end-to-end flow via the Admin UI against a local AppHost run, including bypass scenarios (closed window, capacity at limit, restricted email domain) and a Cancelled-event rejection.
- [x] 6.5 Run `openspec validate admin-add-registration --strict` and resolve any findings.
