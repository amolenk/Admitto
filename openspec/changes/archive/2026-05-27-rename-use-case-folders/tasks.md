## 1. Organization Module

- [x] 1.1 `git mv` `ApiKeyManagement` → `ApiKeys` in src and in `tests/Admitto.Api.Tests/Organization/`; update namespace declarations inside; update `using` statements codebase-wide
- [x] 1.2 `git mv` `TeamManagement` → `Teams` in src and in `tests/Admitto.Core.IntegrationTests/Organization/Application/UseCases/`; update namespace declarations inside; update `using` statements codebase-wide
- [x] 1.3 `git mv` `TeamMembershipManagement` → `TeamMemberships` in src and in `tests/Admitto.Core.IntegrationTests/Organization/Application/UseCases/`; update namespace declarations inside; update `using` statements codebase-wide
- [x] 1.4 `git mv` `TicketedEventManagement` → `TicketedEvents` in src, `tests/Admitto.Core.IntegrationTests/Organization/Application/UseCases/`, and `tests/Admitto.Api.Tests/Organization/`; update namespace declarations inside; update `using` statements codebase-wide
- [x] 1.5 Run `dotnet build` — confirm zero compilation errors for Organization module changes

## 2. Registrations Module

- [x] 2.1 `git mv` `CouponManagement` → `Coupons` in src and in `tests/Admitto.Core.IntegrationTests/Registrations/Application/UseCases/`; update namespace declarations inside; update `using` statements codebase-wide
- [x] 2.2 `git mv` `TicketedEventManagement` → `TicketedEvents` in src and in `tests/Admitto.Core.IntegrationTests/Registrations/Application/UseCases/`; update namespace declarations inside; update `using` statements codebase-wide
- [x] 2.3 `git mv` `TicketTypeManagement` → `TicketTypes` in src, `tests/Admitto.Core.IntegrationTests/Registrations/Application/UseCases/`, and `tests/Admitto.Api.Tests/Registrations/`; update namespace declarations inside; update `using` statements codebase-wide
- [x] 2.4 `git mv` `Waitlist` → `Waitlists` in src and in `tests/Admitto.Core.IntegrationTests/Registrations/Application/UseCases/`; update namespace declarations inside; update `using` statements codebase-wide
- [x] 2.5 Run `dotnet build` — confirm zero compilation errors for Registrations module changes

## 3. Email Module

- [x] 3.1 Create `Emails/` parent group folder under `Email/Application/UseCases/` (src) and under `tests/Admitto.Core.IntegrationTests/Email/Application/UseCases/`
- [x] 3.2 `git mv` `SendEmail/` → `Emails/SendEmail/` in src and tests; update namespace from `...UseCases.SendEmail` → `...UseCases.Emails.SendEmail` inside; update `using` statements codebase-wide
- [x] 3.3 `git mv` `AttendeeEmails/GetAttendeeEmails/` → `Emails/GetAttendeeEmails/` in src and tests; update namespace from `...UseCases.AttendeeEmails` → `...UseCases.Emails` inside; update `using` statements codebase-wide; rename `tests/Admitto.Api.Tests/Registrations/GetAttendeeEmails/` if present
- [x] 3.4 Delete now-empty `AttendeeEmails/` folders in src and tests
- [x] 3.5 Run `dotnet build` — confirm zero compilation errors for Email module changes

## 4. Badges Module

- [x] 4.1 `git mv` `BadgeInstanceManagement` → `BadgeInstances` in src and in `tests/Admitto.Api.Tests/Badges/`; update namespace declarations inside; update `using` statements codebase-wide
- [x] 4.2 `git mv` `BadgeTypeManagement` → `BadgeTypes` in src and in `tests/Admitto.Api.Tests/Badges/`; update namespace declarations inside; update `using` statements codebase-wide
- [x] 4.3 Run `dotnet build` — confirm zero compilation errors for Badges module changes

## 5. Verification

- [x] 5.1 Run architecture tests: `dotnet test tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj`
- [x] 5.2 Run domain tests: `dotnet test tests/Admitto.Core.DomainTests/Admitto.Core.DomainTests.csproj`
- [x] 5.3 Run integration tests: `dotnet test tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj`
- [x] 5.4 Run API tests: `dotnet test tests/Admitto.Api.Tests/Admitto.Api.Tests.csproj`
