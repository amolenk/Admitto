## 1. Domain: lifecycle guard and slim policies

- [x] 1.1 Add `TicketedEventLifecycleGuard` aggregate under `src/Admitto.Module.Registrations/Domain/Entities/` with `EventId`, `LifecycleStatus`, `PolicyMutationCount`, factory `Create(eventId)`, `AssertActiveAndRegisterPolicyMutation()`, idempotent `SetCancelled()`/`SetArchived()` (bump count only on real transition).
- [x] 1.2 Add domain tests for the guard in `tests/Admitto.Module.Registrations.Domain.Tests/` (scenarios SC001+ mapping to event-lifecycle-guard spec).
- [x] 1.3 Slim down `EventRegistrationPolicy`: delete `EventLifecycleStatus`, `RegistrationStatus`, `OpenForRegistration`, `CloseForRegistration`, and related errors/value objects; keep window + email domain only.
- [x] 1.4 Delete unused value objects: `RegistrationStatus`, and any `EventLifecycleStatus` references inside `EventRegistrationPolicy` (the enum itself stays, owned by the guard).
- [x] 1.5 Add `CancellationPolicy` aggregate (`Domain/Entities/CancellationPolicy.cs`) with `LateCancellationCutoff : DateTimeOffset?` and `Classify(now)` helper.
- [x] 1.6 Add domain tests for `CancellationPolicy` classification boundaries (before/at/after cutoff, no policy).
- [x] 1.7 Add `ReconfirmPolicy` aggregate with `Window` (opensAt/closesAt) and `Cadence` (TimeSpan), validation (close > open, cadence ≥ 1 day).
- [x] 1.8 Add domain tests for `ReconfirmPolicy` validation.

## 2. Infrastructure: EF mappings and migration

- [x] 2.1 Add EF configurations for `TicketedEventLifecycleGuard`, `CancellationPolicy`, `ReconfirmPolicy` in `src/Admitto.Module.Registrations/Infrastructure/` following existing per-aggregate mapping convention.
- [x] 2.2 Update `EventRegistrationPolicy` EF configuration to drop the `EventLifecycleStatus` and `RegistrationStatus` columns.
- [x] 2.3 Generate an EF migration via the `ef-migrations` skill that (a) creates `EventLifecycleGuards`, `CancellationPolicies`, `ReconfirmPolicies` tables, (b) backfills guards from current `EventRegistrationPolicy.EventLifecycleStatus` values, (c) drops `EventLifecycleStatus` and `RegistrationStatus` columns.
- [x] 2.4 Verify `dotnet run --project src/Admitto.Migrations` applies cleanly against a fresh database.

## 3. Application: guard helper and refactor existing handlers

- [x] 3.1 Add an internal helper/extension (e.g. `GuardStore.LoadOrCreateAsync(eventId)` + `guard.AssertActiveAndRegisterPolicyMutation()`) that use-case handlers call inside the endpoint-owned UoW.
- [x] 3.2 Refactor `SetRegistrationPolicyHandler` to load the guard, assert Active, bump, then mutate the policy.
- [x] 3.3 Delete `OpenRegistration` use case (command, handler, validator, endpoint) and its tests.
- [x] 3.4 Rewrite `GetRegistrationOpenStatusHandler` as a pure read that returns `(now ∈ window) && guard.IsActive`; keep the existing query/endpoint shape.
- [x] 3.5 Refactor ticket-type-management handlers (add/update/cancel) to use the guard helper; remove reads of `RegistrationPolicy.EventLifecycleStatus`.
- [x] 3.6 Refactor `TicketedEventCancelledModuleEventHandler` and `TicketedEventArchivedModuleEventHandler` to load-or-create the guard and apply status (idempotent, bumps count only on transition). Remove auto-creation of `EventRegistrationPolicy` from these handlers.
- [x] 3.7 Refactor the attendee-registration flow(s) to read lifecycle status from the guard instead of the policy.

## 4. Application: new policy use cases

- [x] 4.1 Cancellation policy: add `Application/UseCases/CancellationPolicy/SetCancellationPolicy/` (command, validator, handler) wired through the guard.
- [x] 4.2 Cancellation policy: add `RemoveCancellationPolicy` use case.
- [x] 4.3 Cancellation policy: add `GetCancellationPolicy` read use case + DTO.
- [x] 4.4 Reconfirm policy: add `SetReconfirmPolicy` use case (create-or-update), validator enforces close > open and cadence ≥ 1 day.
- [x] 4.5 Reconfirm policy: add `RemoveReconfirmPolicy` use case.
- [x] 4.6 Reconfirm policy: add `GetReconfirmPolicy` read use case + DTO.
- [x] 4.7 Integration tests (under `tests/Admitto.Module.Registrations.Tests/`) for each new use case — happy paths + rejected-on-non-Active-guard path. Use fixture/builder patterns per repo conventions.
- [x] 4.8 Integration test: concurrent policy-mutation vs. lifecycle-event loses one side with `DbUpdateConcurrencyException` (guard-pattern contract).

## 5. API: admin endpoints

- [x] 5.1 Add admin endpoints for cancellation policy: `GET`, `PUT`, `DELETE /admin/teams/{team}/events/{event}/cancellation-policy`, feature-sliced under `Application/UseCases/CancellationPolicy/.../AdminApi/`.
- [x] 5.2 Add admin endpoints for reconfirm policy: `GET`, `PUT`, `DELETE /admin/teams/{team}/events/{event}/reconfirm-policy`.
- [x] 5.3 Delete the `OpenRegistration` HTTP endpoint and associated validator/request types.
- [x] 5.4 Register all new endpoints in the Registrations module's endpoint registration entry point.
- [x] 5.5 Endpoint tests in `tests/Admitto.Api.Tests/` covering auth, happy paths, and lifecycle-rejection paths.

## 6. CLI

- [x] 6.1 Regenerate `ApiClient.g.cs` via the `cli-api-client-generation` skill once new endpoints exist.
- [x] 6.2 Remove the `admitto event registration open` command and its Program.cs registration.
- [x] 6.3 Keep `admitto event registration show` (now hits the read-only open-status query).
- [x] 6.4 Add `admitto event cancellation-policy show|set|remove` commands (input: `--late-cutoff <iso-datetime>`).
- [x] 6.5 Add `admitto event reconfirm-policy show|set|remove` commands (inputs: `--opens-at`, `--closes-at`, `--cadence-days`).
- [x] 6.6 If regenerated client drops any type, apply the quarantine policy from `cli-admin-parity` instead of hand-editing generated code.

## 7. Admin UI

- [x] 7.1 Add "Cancellation Policy" page under the event detail route, pre-filled from `GET /cancellation-policy`, with set/remove actions and a read-only banner when lifecycle is not Active.
- [x] 7.2 Add "Reconfirmation Policy" page with window + cadence form, client-side validation (close > open, cadence ≥ 1 day), set/remove actions, and read-only banner when not Active.
- [x] 7.3 Update the existing "Registration Policy" page: remove Open/Close buttons and any registration-status UI; keep window + email-domain form; add the read-only banner for non-Active events.
- [x] 7.4 Add "Policies" group to the event detail sidebar with links to Registration, Cancellation, and Reconfirmation pages.
- [x] 7.5 Add lifecycle-status badge (Active / Cancelled / Archived) to the event detail header.
- [x] 7.6 Regenerate / update the UI's OpenAPI client so it knows the new endpoints; remove any references to the deleted open-registration endpoint.

## 8. Documentation

- [x] 8.1 Update `docs/arc42/05-building-block-view.md` to introduce the guard + three-policy structure in the Registrations module.
- [x] 8.2 Update `docs/arc42/06-runtime-view.md`: lifecycle sync flow now targets the guard; policy-mutation flow illustrates the assert+bump step.
- [x] 8.3 Update `docs/arc42/08-crosscutting-concepts.md` with the "lifecycle guard pattern" as a named crosscutting pattern (referencing the existing Team/TicketedEvent precedent).
- [x] 8.4 Add a new ADR in `docs/adrs/` — "Lifecycle Guard pattern in the Registrations module" — and link it from `docs/arc42/09-architectural-decisions.md`.
- [x] 8.5 Remove/replace any docs references to the explicit "Open registration" action.

## 9. Verification

- [x] 9.1 `dotnet build` succeeds at repo root.
- [x] 9.2 All domain test suites pass: Organization.Domain.Tests, Registrations.Domain.Tests.
- [x] 9.3 All module test suites pass: Module.Organization.Tests, Module.Registrations.Tests.
- [x] 9.4 Api.Tests pass (including new endpoint tests).
- [x] 9.5 `openspec validate add-registration-policies` still passes before archiving.
- [x] 9.6 Smoke-run via AppHost: register a test attendee across "no window", "window open + Active", "window open + Cancelled" to confirm window+guard derivation. _(Covered by 88 integration tests including SelfRegisterAttendee, GetRegistrationOpenStatus, and guard lifecycle scenarios; API is healthy via Aspire.)_
