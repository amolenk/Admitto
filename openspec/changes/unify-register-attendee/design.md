## Context

Three sibling use cases under `src/Admitto.Module.Registrations/Application/UseCases/Registrations/` (`AdminRegisterAttendee`, `SelfRegisterAttendee`, `RegisterWithCoupon`) each define their own command, handler, and HTTP endpoint, but build the same `Registration` aggregate from the same inputs. The three handlers already share helpers via static cross-references on `SelfRegisterAttendeeHandler` (its `Errors` and `ValidateTicketTypeSelection` are reused by both other handlers), making the de-facto "shared core" implicit.

Differences between the three paths:

| Concern                  | Self-service | Admin-add | Coupon                       |
|--------------------------|--------------|-----------|------------------------------|
| Email-verification token | **required** | bypass    | bypass                        |
| Email == coupon target   | n/a          | n/a       | **enforce**                   |
| Registration window      | enforce      | bypass    | enforce unless coupon flag    |
| Email-domain restriction | enforce      | bypass    | bypass                        |
| Per-type capacity        | enforce      | bypass    | bypass                        |
| Coupon lookup/redeem     | n/a          | n/a       | required                     |
| `TicketCatalog.Claim` enforce flag | `true` | `false`  | `false`                      |
| `TicketedEvent.Status` active gate | yes  | yes      | yes                           |
| `TicketCatalog.EventStatus` safety net | yes | yes  | yes                           |
| Ticket-type validation   | yes          | yes       | yes                           |
| Additional-details validation | yes     | yes       | yes                           |
| Authorization            | anonymous    | Organizer team membership | anonymous       |
| HTTP route               | `POST /teams/{teamSlug}/events/{eventSlug}/registrations` | `POST /admin/teams/{teamSlug}/events/{eventSlug}/registrations` | `POST /teams/{teamSlug}/events/{eventSlug}/registrations/coupon` |

A single command/handler can express the matrix above with a `RegistrationMode` enum and an optional `CouponCode`. The HTTP layer continues to provide three distinct endpoints (different routes, requests, and authorization), each constructing the same command.

## Goals / Non-Goals

**Goals:**

- One `RegisterAttendeeCommand` and `RegisterAttendeeHandler` that encode all current behavior of the three handlers.
- Endpoint folder layout that already exists (`RegisterAttendee/AdminApi/` and `RegisterAttendee/PublicApi/`) is filled in; legacy folders deleted.
- All existing acceptance scenarios in `attendee-registration`, `admin-registration`, and `coupon-management` pass unchanged, plus the new self-service-verification and coupon-email-binding scenarios.
- Errors centralized on the new handler; no cross-handler static references remain.
- HTTP routes, response shapes, and authorization unchanged. Self-service request gains an optional `emailVerificationToken` field.
- The contract for email-verification is in place: `IEmailVerificationTokenValidator` interface declared, a placeholder implementation registered in DI that throws `NotImplementedException`, command field present, handler invokes it in self-service mode.

**Non-Goals:**

- No domain model changes (`Registration`, `TicketCatalog`, `Coupon`, `TicketedEvent`).
- No implementation of email-verification token *issuance* (verify-link generation, email delivery, signing key management).
- No working implementation of `IEmailVerificationTokenValidator`; only a placeholder that throws `NotImplementedException` is registered, so any self-service request with a non-null token causes a 500-class failure until the real validator ships. Self-service requests without a token still get the canonical `email.verification_required` validation error.
- No CLI behavior changes; `ApiClient.g.cs` is regenerated only if the OpenAPI operation `name` actually changes.
- No new authorization model; the public endpoints stay anonymous and the admin endpoint stays gated by `RequireTeamMembership(TeamMembershipRole.Organizer)`.
- No consolidation of CLI commands (`AddRegistrationCommand` keeps its current shape).
- No per-event opt-in for coupon verification tokens (a `Coupon.RequiresVerificationToken` flag is a future change).

## Decisions

### D1. Discriminate paths with an explicit `RegistrationMode` enum

The command is:

```csharp
internal sealed record RegisterAttendeeCommand(
    TicketedEventId EventId,
    EmailAddress Email,
    string[] TicketTypeSlugs,
    RegistrationMode Mode,
    string? CouponCode = null,
    string? EmailVerificationToken = null,
    IReadOnlyDictionary<string, string>? AdditionalDetails = null) : Command<RegistrationId>;

internal enum RegistrationMode { SelfService, AdminAdd, Coupon }
```

Rationale: explicit > implicit. Reading the call site at the endpoint immediately tells you which path runs. `CouponCode` is required when `Mode == Coupon` and ignored otherwise; `EmailVerificationToken` is required when `Mode == SelfService` and ignored otherwise. The handler asserts both invariants and the per-endpoint construction guarantees them. Alternatives considered: `IsAdmin` bool + nullable coupon (overloaded semantics; "admin coupon" is undefined); discriminated union of three command records (more types for little gain since the handler body is mostly shared anyway).

### D2. Single linear handler with mode-driven branches, not a strategy hierarchy

The handler executes the union of steps in a fixed order, gating per-mode-only steps with `switch`/`if (mode == ...)`:

1. **(SelfService only)** Validate email-verification token via `IEmailVerificationTokenValidator`:
   - `EmailVerificationToken is null` → reject with `email.verification_required`.
   - Validator returns failure (bad signature, expired, or embedded email ≠ `command.Email`) → reject with `email.verification_invalid`.
2. If `Coupon` mode: load coupon, validate status (Expired/Redeemed/Revoked), validate ticket-type allowlist, **and assert `command.Email == coupon.TargetEmail` (else `coupon.email_mismatch`)**.
3. Load `TicketedEvent` (404 → `EventNotFound`).
4. Active-status gate: reject if `!ticketedEvent.IsActive`.
5. Window/domain checks:
   - `SelfService`: enforce window + domain.
   - `Coupon`: enforce window unless `coupon.BypassRegistrationWindow`; never enforce domain.
   - `AdminAdd`: skip both.
6. Load `TicketCatalog` (handle missing per mode: `SelfService`/`AdminAdd` → `NoTicketTypesConfigured`; `Coupon` may proceed without a catalog, mirroring current behavior).
7. Validate ticket-type selection against the catalog (shared helper, now a private method on the new handler).
8. Build `TicketTypeSnapshot`s.
9. `catalog.Claim(slugs, enforce: mode == SelfService)`; translate `TicketCatalog.Errors.EventNotActive` into the local `EventNotActive` error.
10. If `Coupon` mode: `coupon.Redeem()`.
11. Validate and apply `AdditionalDetails`.
12. `Registration.Create(...)`; add to write store; return `Id`.

Rationale: a tiny strategy hierarchy (`IRegistrationStrategy`) would scatter the linear flow across three classes for behavior that is genuinely a step-by-step pipeline with three conditional branches. The current code is short enough that one handler with explicit branches is clearer than indirection.

### D3. Centralize errors on the new handler

All `Error` constants currently on `SelfRegisterAttendeeHandler.Errors` and `RegisterWithCouponHandler.Errors` move to `RegisterAttendeeHandler.Errors`. Existing error codes (`registration.event_not_found`, `coupon.expired`, etc.) remain identical so API responses are byte-for-byte compatible. Three new error codes are introduced for the email-verification work:

- `email.verification_required` (Validation) — self-service request without a token.
- `email.verification_invalid` (Validation) — token failed signature/expiry/email-binding check.
- `coupon.email_mismatch` (Validation) — coupon mode where `command.Email != coupon.TargetEmail`.

Tests update their `using`/reference targets accordingly.

### D7. Email-verification validator extension point

A new application-layer interface lives at `src/Admitto.Module.Registrations/Application/Security/IEmailVerificationTokenValidator.cs`:

```csharp
internal interface IEmailVerificationTokenValidator
{
    ValueTask<EmailVerificationResult> ValidateAsync(
        string token, EmailAddress expectedEmail, CancellationToken cancellationToken);
}

internal readonly record struct EmailVerificationResult(bool IsValid, string? FailureReason = null);
```

Rationale:

- The interface is declared and a **placeholder implementation is registered** in the module's DI composition root (`RegistrationsModule` / `Application/DependencyInjection.cs`):

  ```csharp
  internal sealed class NotImplementedEmailVerificationTokenValidator : IEmailVerificationTokenValidator
  {
      public ValueTask<EmailVerificationResult> ValidateAsync(
          string token, EmailAddress expectedEmail, CancellationToken cancellationToken)
          => throw new NotImplementedException(
              "Email-verification token validation has not been implemented yet. "
              + "Replace this registration with the real validator before enabling self-service registration.");
  }
  ```

  The handler constructor-injects `IEmailVerificationTokenValidator` like any normal dependency — no `IServiceProvider`, no `GetService<>()`, no nullable parameter, no lazy resolution.
- Behavior at runtime in this change:
  - Self-service request **without** a token → handler short-circuits with `email.verification_required` *before* calling the validator (the null check happens first). No exception.
  - Self-service request **with** a token → handler calls the validator, which throws `NotImplementedException` and surfaces as a 500-class error. This is the deliberate hard-fail until the issuer + real validator land.
  - Coupon and admin modes never call the validator.
- When the real validator ships, the only change required is swapping the DI registration; no handler or command changes.

### D8. Coupon target-email binding

The coupon aggregate already stores the `TargetEmail` it was issued for. In the unified handler, after the coupon is loaded and its status/allowlist are validated, we compare:

```csharp
if (coupon.TargetEmail != command.Email)
    throw new BusinessRuleViolationException(Errors.CouponEmailMismatch);
```

Rationale: the coupon code was delivered only to `coupon.TargetEmail`; possession of the code is the bearer credential, but binding it to that address closes the trivial "leaked code → register under any email" hole without requiring a separate verification token round-trip for invitees. If higher assurance is needed later, a `RequiresVerificationToken` flag on `Coupon` can opt-in coupon mode to the verification path described in D7 — that is out of scope here.

### D4. Endpoint folder layout

```
Application/UseCases/Registrations/RegisterAttendee/
├── RegisterAttendeeCommand.cs
├── RegisterAttendeeHandler.cs
├── AdminApi/
│   ├── AdminRegisterAttendeeHttpEndpoint.cs
│   ├── AdminRegisterAttendeeHttpRequest.cs
│   ├── AdminRegisterAttendeeHttpResponse.cs
│   └── AdminRegisterAttendeeValidator.cs
└── PublicApi/
    ├── SelfService/
    │   ├── SelfRegisterAttendeeHttpEndpoint.cs
    │   ├── SelfRegisterAttendeeHttpRequest.cs
    │   └── SelfRegisterAttendeeValidator.cs
    └── Coupon/
        ├── RegisterWithCouponHttpEndpoint.cs
        ├── RegisterWithCouponHttpRequest.cs
        └── RegisterWithCouponValidator.cs
```

Endpoint type names, route names (`WithName(...)`), request DTO names, and the response DTO are kept identical to today so the OpenAPI document and the regenerated `ApiClient.g.cs` are unchanged. Only the C# namespaces shift to mirror the new folder layout.

### D5. Test reorganization mirrors production layout

Per-handler integration tests (`Admitto.Module.Registrations.Tests`) and per-endpoint API tests (`Admitto.Api.Tests`) move under `RegisterAttendee/{AdminApi,PublicApi/SelfService,PublicApi/Coupon}/` with one fixture + one tests class per channel. The handler-level test classes are merged into a single `RegisterAttendeeHandlerTests` class organized by `[Theory]` over `RegistrationMode` where the assertion is identical, and per-mode `[Fact]`s where behavior diverges. All existing `SC###`-prefixed scenario tests are preserved and renamed only to fit the new class.

### D6. `RegistrationsModule.cs` wiring

`AddModuleEventHandlersFromAssembly` already auto-registers handlers; the new `RegisterAttendeeHandler` is picked up automatically. The endpoint-mapping entry point that today calls `MapAdminRegisterAttendee`, `MapSelfRegisterAttendee`, `MapRegisterWithCoupon` is updated to call the equivalent extension methods on the new endpoint classes (names preserved).

## Risks / Trade-offs

- **[Risk] Behavior regression on a subtle path (e.g. coupon-without-catalog, claim safety net, error mapping).** → Mitigation: keep all existing scenario tests and run the targeted suites listed in `AGENTS.md` before declaring done; preserve error codes verbatim so any contract test still passes.
- **[Risk] OpenAPI operation name change leaks into `ApiClient.g.cs` and breaks the CLI.** → Mitigation: keep `WithName(nameof(AdminRegisterAttendee))`-equivalent names identical; if any operation name changes, run `generate-api-client.sh` and verify `AddRegistrationCommand` still compiles.
- **[Risk] Self-service requests that *do* supply a token will throw `NotImplementedException` (500) the moment this lands, until the email-verification *issuer* + real validator are implemented.** → Mitigation: this is intentional per D7 (ship the contract, force the issuer to land). Self-service requests *without* a token still get a clean `400 email.verification_required`. Communicate clearly in the PR description; do not deploy this change to production until the companion issuer change is also ready.
- **[Risk] Existing consumers of the public self-service endpoint (Admin UI public-facing pages, integration tests, the design mock) will start receiving `400 email.verification_required` (or `500` if they happen to send a token).** → Mitigation: the API tests for self-service in this change are written to assert the new rejection; any UI/E2E tests outside this repo's `tests/` tree may need to be paused or stubbed until the issuer lands. List them in the PR.
- **[Risk] Coupon target-email mismatch becomes a hard rejection where it was silently accepted.** → Mitigation: this is the intended security improvement; the `coupon-management` invitation email already includes the target email, so legitimate users will use the right address. A migration note in the PR is sufficient — there is no data migration since coupons already store `TargetEmail`.
- **[Trade-off] One handler with branches vs. three thin classes.** → We accept slightly denser handler in exchange for one place to read and maintain the registration flow.
- **[Trade-off] `CouponCode` and `EmailVerificationToken` both live on the command for non-applicable modes.** → Acceptable; the handler enforces the invariants and only the relevant endpoint sets each field.

## Migration Plan

This is an in-repo refactor on a single commit/PR; no runtime migration is required.

1. Add the new command, handler, endpoints, and tests.
2. Wire the new endpoints into the module's endpoint-registration entry point.
3. Run targeted module tests + API tests; fix regressions.
4. Delete the three legacy folders (production + test).
5. Regenerate `ApiClient.g.cs` only if `dotnet run --project src/Admitto.Api` produces a different OpenAPI document.

Rollback: revert the PR; the legacy code is restored.
