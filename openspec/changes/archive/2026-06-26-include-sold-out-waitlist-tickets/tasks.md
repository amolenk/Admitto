## 1. Domain

- [x] 1.1 Add a `TicketType.IsSoldOut` computed property defined as `MaxCapacity is not null && UsedCapacity >= MaxCapacity.Value`.
- [x] 1.2 (Optional) Replace the duplicated inline sold-out comparisons in `TicketType` / `TicketCatalog` with `IsSoldOut` where it does not change behavior.
- [x] 1.3 Add/extend domain tests for `IsSoldOut` covering: bounded-and-at-capacity, bounded-and-under-capacity, and null-capacity (never sold out).

## 2. Public Contract

- [x] 2.1 Update `PublicTicketTypeDto` to expose `soldOut` (replacing `maxCapacity` / `usedCapacity`) and `requiresWaitlist` (replacing `waitlistEnabled` / `waitlistMode`).
- [x] 2.2 Update `GetPublicTicketTypesHandler` to map `soldOut = ticketType.IsSoldOut` and `requiresWaitlist = ticketType.WaitlistMode`, keeping the existing `SelfServiceEnabled` filter and not adding any capacity filter.

## 3. Tests

- [x] 3.1 API test: available ticket returns `soldOut = false`, `requiresWaitlist = false`.
- [x] 3.2 API test: ticket with no configured capacity returns `soldOut = false`, `requiresWaitlist = false`.
- [x] 3.3 API test: sold-out waitlistable ticket returns `soldOut = true`, `requiresWaitlist = true`.
- [x] 3.4 API test: sold-out non-waitlist ticket returns `soldOut = true`, `requiresWaitlist = false`.
- [x] 3.5 API test: response no longer exposes `maxCapacity`, `usedCapacity`, `waitlistEnabled`, or `waitlistMode`.
- [x] 3.6 Update existing `PublicTicketTypesTests` assertions to the new field names.

## 4. SDK / OpenAPI Regeneration

- [x] 4.1 Start the stack and regenerate the OpenAPI spec per `AGENTS.md` ("Regenerating the Admin UI SDK"): `aspire start --isolated`, `aspire wait api`, then fetch `/openapi/v1.json` to `src/Admitto.UI.Admin/openapi-spec.json`.
- [x] 4.2 Regenerate the Admin UI SDK: `cd src/Admitto.UI.Admin && pnpm openapi-ts`.
- [x] 4.3 Update any Admin UI / proxy consumers of the old public ticket fields to `soldOut` / `requiresWaitlist`.
- [x] 4.4 Confirm no other generated client (e.g. CLI `ApiClient`) consumes the public ticket-type response; regenerate only if one does.

## 5. Verification

- [x] 5.1 Run architecture tests first: `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj`.
- [x] 5.2 Run domain tests for `IsSoldOut`: `dotnet test --project tests/Admitto.Core.DomainTests/Admitto.Core.DomainTests.csproj`.
- [x] 5.3 Run public ticket-type API tests: `dotnet test --project tests/Admitto.Api.Tests/Admitto.Api.Tests.csproj`.
