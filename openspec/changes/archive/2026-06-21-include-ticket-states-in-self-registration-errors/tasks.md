## 1. Contract and Classification

- [x] 1.1 Define the public ticket-state conflict response shape for self-service registration, including stable code and grouped ticket-state id arrays.
- [x] 1.2 Add application-level classification logic that maps submitted `registerTicketTypeIds` and `waitlistTicketTypeIds` against the current `TicketCatalog` state without mutating capacity or waitlists.
- [x] 1.3 Ensure classification runs only after email-verification token validation succeeds and does not run for token failures.

## 2. Self-Service Registration Flow

- [x] 2.1 Update self-service registration handling so recoverable ticket-selection mismatches return the structured ticket-state conflict instead of only the first generic domain error.
- [x] 2.2 Preserve existing all-or-nothing behavior for failed submissions: no registration, waitlist entry, or capacity change is persisted.
- [x] 2.3 Preserve existing terminal errors for event inactive, registration window, email-domain, duplicate registration, and additional-detail validation failures.

## 3. HTTP and OpenAPI Surface

- [x] 3.1 Expose the structured ticket-state conflict through the public registration endpoint as HTTP 409 problem details with additive extensions.
- [x] 3.2 Ensure the OpenAPI description includes the new conflict response schema for self-service registration.
- [x] 3.3 Regenerate any affected generated API clients after the backend contract is available.

## 4. Tests

- [x] 4.1 Add handler or integration coverage for a requested registration ticket that became waitlistable.
- [x] 4.2 Add coverage for a requested registration ticket that became unavailable without waitlist.
- [x] 4.3 Add coverage for a requested waitlist ticket that became registerable again.
- [x] 4.4 Add coverage that mixed selections report submitted ticket ids in grouped state arrays and persist no partial changes.
- [x] 4.5 Add API coverage that token failures do not include ticket-state details.
- [x] 4.6 Run `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj` before other test suites.
- [x] 4.7 Run targeted Registrations/API tests covering self-service registration conflicts.
