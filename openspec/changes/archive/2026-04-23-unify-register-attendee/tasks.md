## 1. Shared command and handler

- [x] 1.1 Create `RegisterAttendee/RegisterAttendeeCommand.cs` with `EventId`, `Email`, `TicketTypeSlugs`, `Mode` (`RegistrationMode` enum), optional `CouponCode`, optional `EmailVerificationToken`, optional `AdditionalDetails`; result is `RegistrationId`.
- [x] 1.2 Create `RegisterAttendee/RegistrationMode.cs` with values `SelfService`, `AdminAdd`, `Coupon`.
- [x] 1.3 Create `Application/Security/IEmailVerificationTokenValidator.cs` (interface) plus the `EmailVerificationResult` record. Add `NotImplementedEmailVerificationTokenValidator` (placeholder implementation that throws `NotImplementedException` from `ValidateAsync`) and register it in the module's `Application/DependencyInjection.cs` as a singleton `IEmailVerificationTokenValidator`. Document inline why it throws and that swapping the registration is the only step needed when the real validator ships.
- [x] 1.4 Create `RegisterAttendee/RegisterAttendeeHandler.cs` implementing the linear flow from design D2: verification check (self-service) → coupon load + status + allowlist + **target-email match** (coupon) → event load → active gate → window/domain (mode-gated) → catalog load → ticket-type validation → snapshots → claim with `enforce: mode == SelfService` → coupon redeem (coupon mode) → additional-details validation → `Registration.Create` → add to write store.
- [x] 1.5 In the handler, constructor-inject `IEmailVerificationTokenValidator` (non-nullable). The verification step MUST run before any event/catalog/coupon load. If `EmailVerificationToken is null` → reject with `email.verification_required` *without* calling the validator. Otherwise call the validator; a `false` result maps to `email.verification_invalid`. (In this change, calling the validator with a non-null token will throw `NotImplementedException` from the placeholder; that is intentional — see D7.)
- [x] 1.6 Centralize all error constants on `RegisterAttendeeHandler.Errors` with the same codes as today (`registration.event_not_found`, `registration.event_not_active`, `registration.not_open`, `registration.closed`, `registration.email_domain_not_allowed`, `registration.no_ticket_types`, `registration.duplicate_ticket_types`, `registration.unknown_ticket_types`, `registration.cancelled_ticket_types`, `registration.overlapping_time_slots`, `coupon.not_found`, `coupon.expired`, `coupon.already_redeemed`, `coupon.revoked`, `coupon.ticket_type_not_allowed`) plus three new codes: `email.verification_required`, `email.verification_invalid`, `coupon.email_mismatch`.
- [x] 1.7 Move `ValidateTicketTypeSelection` into the new handler as a private static method.
- [x] 1.8 Assert command invariants in the handler: `Mode == Coupon` ⇔ `CouponCode is not null`; `Mode == SelfService` ⇒ verification step runs (token may still be null at the command boundary, the handler just rejects).

## 2. HTTP endpoints (port without behavior change, plus new self-service field)

- [x] 2.1 Create `RegisterAttendee/AdminApi/AdminRegisterAttendeeHttpRequest.cs`, `AdminRegisterAttendeeValidator.cs`, `AdminRegisterAttendeeHttpResponse.cs` (move from old folder; keep type names, properties, and validation rules identical).
- [x] 2.2 Create `RegisterAttendee/AdminApi/AdminRegisterAttendeeHttpEndpoint.cs` mapping `POST /registrations` (under the admin group), keeping `WithName(nameof(AdminRegisterAttendee))` and `RequireAuthorization(... TeamMembershipRole.Organizer)`. Build a `RegisterAttendeeCommand` with `Mode = AdminAdd`, `CouponCode = null`, `EmailVerificationToken = null`.
- [x] 2.3 Create `RegisterAttendee/PublicApi/SelfService/SelfRegisterAttendeeHttpRequest.cs` adding an optional `string? EmailVerificationToken` field, plus `SelfRegisterAttendeeValidator.cs` (validation of the token shape itself is delegated to the validator service; the FluentValidator does not require the token to be non-empty so the handler can produce the canonical `email.verification_required` error).
- [x] 2.4 Create `RegisterAttendee/PublicApi/SelfService/SelfRegisterAttendeeHttpEndpoint.cs` mapping `POST /registrations` (under the public team/event group), keeping `WithName(nameof(SelfRegisterAttendeeHttpEndpoint))`. Build a `RegisterAttendeeCommand` with `Mode = SelfService` and `EmailVerificationToken = request.EmailVerificationToken`.
- [x] 2.5 Create `RegisterAttendee/PublicApi/Coupon/RegisterWithCouponHttpRequest.cs` and `RegisterWithCouponValidator.cs` (port from old folder, identical — no token field).
- [x] 2.6 Create `RegisterAttendee/PublicApi/Coupon/RegisterWithCouponHttpEndpoint.cs` mapping `POST /registrations/coupon`, keeping `WithName(nameof(RegisterWithCouponHttpEndpoint))`. Build a `RegisterAttendeeCommand` with `Mode = Coupon`, the request's `CouponCode`, and `EmailVerificationToken = null`.
- [x] 2.7 Update endpoint-registration entry point in `RegistrationsModule.cs` (or wherever the three `Map*` extensions are wired) to call `MapAdminRegisterAttendee` / `MapSelfRegisterAttendee` / `MapRegisterWithCoupon` from the new endpoint classes.

## 3. Test reorganization

- [x] 3.1 Create `tests/Admitto.Module.Registrations.Tests/Application/UseCases/Registrations/RegisterAttendee/` with a `RegisterAttendeeHandlerFixture` consolidating the three legacy fixtures. The fixture MUST inject a fake `IEmailVerificationTokenValidator` into the test container (overriding the production placeholder), defaulting to a stub that succeeds when `token == "VALID-TOKEN-FOR-{email}"` so tests are self-explanatory.
- [x] 3.2 Create `RegisterAttendeeHandlerTests` covering all scenarios from `SelfRegisterAttendeeTests`, `AdminRegisterAttendeeTests`, and `RegisterWithCouponTests`. Use `[Theory]` over `RegistrationMode` for shared assertions (active-gate, ticket-type validation, additional-details validation, claim safety net) and `[Fact]` for mode-specific behavior. Preserve every `SC###`-prefixed scenario name.
- [x] 3.3 Add new handler tests for the verification scenarios from the modified `attendee-registration` spec: token-missing → `email.verification_required`; token-invalid → `email.verification_invalid`; verification runs before any other lookup (assert by setting up a non-existent event id and confirming the verification error is returned).
- [x] 3.4 Add new handler tests for the coupon-target-email scenarios: success when emails match; `coupon.email_mismatch` when they don't; coupon does NOT require a verification token.
- [x] 3.5 Reorganize `tests/Admitto.Api.Tests/Registrations/` into `RegisterAttendee/{AdminApi,PublicApi/SelfService,PublicApi/Coupon}/` with one fixture + one tests class per channel (port from `tests/Admitto.Api.Tests/Registrations/AdminRegisterAttendee/` and any equivalent self-service/coupon API tests).
- [x] 3.6 Update self-service API tests to assert the new behavior end-to-end. The test host MUST replace the placeholder `NotImplementedEmailVerificationTokenValidator` with a fake validator (otherwise any request carrying a token throws). Required cases: (a) request with no token → `400 email.verification_required` (validator never invoked, so the placeholder behavior is irrelevant); (b) request with a token accepted by the fake → `201 Created`; (c) request with a token rejected by the fake → `400 email.verification_invalid`.

## 4. Delete legacy code

- [x] 4.1 Delete `src/Admitto.Module.Registrations/Application/UseCases/Registrations/AdminRegisterAttendee/` (folder and all files).
- [x] 4.2 Delete `src/Admitto.Module.Registrations/Application/UseCases/Registrations/SelfRegisterAttendee/` (folder and all files).
- [x] 4.3 Delete `src/Admitto.Module.Registrations/Application/UseCases/Registrations/RegisterWithCoupon/` (folder and all files).
- [x] 4.4 Delete the legacy test folders `tests/Admitto.Module.Registrations.Tests/Application/UseCases/Registrations/{AdminRegisterAttendee,SelfRegisterAttendee,RegisterWithCoupon}/`.
- [x] 4.5 Delete `tests/Admitto.Api.Tests/Registrations/AdminRegisterAttendee/` (and any matching self-service/coupon legacy folders) once their replacements are in place.
- [x] 4.6 Grep for any lingering `AdminRegisterAttendee`, `SelfRegisterAttendee`, or `RegisterWithCoupon` references in production code (excluding endpoint type names that are intentionally preserved for OpenAPI parity) and remove or update them.

## 5. Verify

- [x] 5.1 `dotnet build` the solution; resolve namespace/import errors from the moved files.
- [x] 5.2 `dotnet test tests/Admitto.Module.Registrations.Domain.Tests/Admitto.Module.Registrations.Domain.Tests.csproj` (sanity).
- [x] 5.3 `dotnet test tests/Admitto.Module.Registrations.Tests/Admitto.Module.Registrations.Tests.csproj`.
- [x] 5.4 `dotnet test tests/Admitto.Api.Tests/Admitto.Api.Tests.csproj`.
- [x] 5.5 Diff the generated OpenAPI document against `main`: the self-service request body now includes an optional `emailVerificationToken` field. Run `src/Admitto.Cli/Api/generate-api-client.sh` to refresh `ApiClient.g.cs`. Confirm `src/Admitto.Cli/Commands/Events/Registration/AddRegistrationCommand.cs` (admin path) still compiles; the CLI does not call the public self-service endpoint.
- [x] 5.6 PR description MUST call out: (a) self-service endpoint is intentionally inoperative until the email-verification issuer ships in a follow-up change — requests *without* a token return `400 email.verification_required`, requests *with* a token return `500` from the placeholder `NotImplementedEmailVerificationTokenValidator`; (b) coupon mode now enforces target-email match — list any operator runbook/UI strings that need updating.

