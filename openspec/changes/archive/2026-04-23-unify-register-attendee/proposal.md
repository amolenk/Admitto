## Why

The Registrations module currently has three separate use-case folders that all create a `Registration` aggregate via almost-identical code paths: `AdminRegisterAttendee`, `SelfRegisterAttendee`, and `RegisterWithCoupon`. Each has its own `Command`, `Handler`, and validator, with substantial duplication (event/catalog load, ticket-type validation, claim, additional-details validation, registration creation). The handlers already cross-reference each other (`RegisterWithCouponHandler` calls into `SelfRegisterAttendeeHandler.ValidateTicketTypeSelection`/`Errors`; `AdminRegisterAttendeeHandler` does the same), which leaks the intended shared core out into ad-hoc static helpers.

Consolidating into a single `RegisterAttendee` use case with one `Command`/`Handler` (and per-channel HTTP endpoints sharing it) removes the duplication, makes the per-path behavior differences (window/domain/capacity bypass, coupon redemption) explicit in one place, and matches the existing pattern already scaffolded under `Application/UseCases/Registrations/RegisterAttendee/{AdminApi,PublicApi}/`.

## What Changes

- Introduce a single internal command `RegisterAttendeeCommand` and `RegisterAttendeeHandler` under `Application/UseCases/Registrations/RegisterAttendee/`. The command carries a `RegistrationMode` enum (`SelfService` | `AdminAdd` | `Coupon`), an optional `CouponCode`, and an optional `EmailVerificationToken`; the handler implements the unified policy/claim/coupon-redemption flow.
- Add the email-verification *plumbing* (token issuance is out of scope): introduce `IEmailVerificationTokenValidator` (interface only — no implementation, no DI registration yet). The handler invokes the validator only in `SelfService` mode and rejects the request with `email.verification_required` (missing token) or `email.verification_invalid` (validator failure). Until the validator is registered in DI, all self-service registrations will fail with `email.verification_required` — this is intentional, to force the issuer + validator implementation to land before self-service is exposed.
- For `Coupon` mode, enforce that `command.Email == coupon.TargetEmail`; reject with `coupon.email_mismatch` otherwise. No verification token required (the coupon code, delivered to the target email, is the bearer credential).
- For `AdminAdd` mode, neither verification nor any email-binding check applies.
- Move the three HTTP endpoints into subfolders under the unified use case:
  - `RegisterAttendee/AdminApi/` — `POST /admin/teams/{teamSlug}/events/{eventSlug}/registrations` (was `AdminRegisterAttendee`)
  - `RegisterAttendee/PublicApi/SelfService/` — `POST /teams/{teamSlug}/events/{eventSlug}/registrations` (was `SelfRegisterAttendee`)
  - `RegisterAttendee/PublicApi/Coupon/` — `POST /teams/{teamSlug}/events/{eventSlug}/registrations/coupon` (was `RegisterWithCoupon`)
  Each endpoint maps its own request DTO and validator to the shared `RegisterAttendeeCommand` with the appropriate mode.
- Centralize all registration `Errors` (currently split between `SelfRegisterAttendeeHandler.Errors` and `RegisterWithCouponHandler.Errors`) onto the new handler.
- Delete the old `AdminRegisterAttendee/`, `SelfRegisterAttendee/`, and `RegisterWithCoupon/` folders (production and test) and their commands/handlers, after porting all behavior and tests.
- Reorganize integration tests under `tests/Admitto.Module.Registrations.Tests/Application/UseCases/Registrations/RegisterAttendee/` (mirroring the production layout) and `tests/Admitto.Api.Tests/Registrations/RegisterAttendee/`.
- No change to public HTTP routes, request/response shapes, validation rules, authorization, CLI surface, or NSwag-generated `ApiClient.g.cs`. The OpenAPI operation `name` for the admin endpoint changes (it currently uses `nameof(AdminRegisterAttendee)`); the CLI `ApiClient` will be regenerated to match.

## Capabilities

### New Capabilities
- _none_

### Modified Capabilities
- `attendee-registration`: self-service SHALL require a valid email-verification token; coupon registration SHALL require `command.Email == coupon.TargetEmail`. (Behavior strengthening — this change adds two new rejection paths and the validator extension point.)

## Impact

- **Code**: `src/Admitto.Module.Registrations/Application/UseCases/Registrations/{AdminRegisterAttendee,SelfRegisterAttendee,RegisterWithCoupon}` removed; new shared command/handler under `RegisterAttendee/`. New `Application/Security/IEmailVerificationTokenValidator.cs` interface (no implementation). Tests under `tests/Admitto.Module.Registrations.Tests/Application/UseCases/Registrations/` and `tests/Admitto.Api.Tests/Registrations/` reorganized.
- **Endpoint registration**: `RegistrationsModule.cs` (or wherever the three `Map*` extension methods are wired) updated to call the new endpoint extensions.
- **CLI**: `src/Admitto.Cli/Commands/Events/Registration/AddRegistrationCommand.cs` continues to work via `ApiClient.g.cs`. Regenerate `ApiClient.g.cs` via `generate-api-client.sh` if the operation `name` changes.
- **APIs**: routes, response shapes, and authorization unchanged. Self-service request gains an optional `emailVerificationToken` field; missing/invalid token now produces a `400` with the new error codes (previously the request would have succeeded). Coupon registration now rejects with `coupon.email_mismatch` if the supplied email does not match `coupon.TargetEmail`.
- **Specs**: `attendee-registration` modified — self-service requirement adds a verification-token mandate; coupon requirement adds the target-email match.
