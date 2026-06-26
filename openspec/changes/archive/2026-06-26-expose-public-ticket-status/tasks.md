## 1. Public Contract

- [x] 1.1 Add a public ticket status representation for the exact values `available`, `waitlist`, and `soldOut`.
- [x] 1.2 Update `PublicTicketTypeDto` to expose `status` and remove `soldOut` / `requiresWaitlist` from the public response contract.
- [x] 1.3 Update `GetPublicTicketTypesHandler` to map `status = "waitlist"` when `WaitlistMode` is true, `status = "soldOut"` when `IsSoldOut` is true, and `status = "available"` otherwise.
- [x] 1.4 Keep the existing `SelfServiceEnabled` filter and do not add any capacity/actionability filter.

## 2. Tests

- [x] 2.1 API test: available bounded ticket returns `status = "available"`.
- [x] 2.2 API test: ticket with no configured capacity returns `status = "available"`.
- [x] 2.3 API test: sold-out waitlistable ticket returns `status = "waitlist"`.
- [x] 2.4 API test: sold-out non-waitlist ticket returns `status = "soldOut"`.
- [x] 2.5 API test: response no longer exposes `soldOut`, `requiresWaitlist`, raw capacity counters, or internal waitlist flags.
- [x] 2.6 Update existing `PublicTicketTypesTests` assertions from boolean fields to the new `status` field.

## 3. SDK / Consumer Updates

- [x] 3.1 Start the stack and regenerate the OpenAPI spec per `AGENTS.md`: `aspire start --isolated`, `aspire wait api`, then fetch `/openapi/v1.json` to `src/Admitto.UI.Admin/openapi-spec.json`.
- [x] 3.2 Regenerate the Admin UI SDK: `cd src/Admitto.UI.Admin && pnpm openapi-ts`.
- [x] 3.3 Update any Admin UI / proxy consumers of `soldOut` / `requiresWaitlist` to use `status`.
- [x] 3.4 Confirm no other generated client, including the CLI `ApiClient`, consumes the public ticket-type response; regenerate only if one does.

## 4. Verification

- [x] 4.1 Run architecture tests first: `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj`.
- [x] 4.2 Run public ticket-type API tests: `dotnet test --project tests/Admitto.Api.Tests/Admitto.Api.Tests.csproj`.
