## 1. Domain — Remove Team EmailAddress

- [x] 1.1 Remove `EmailAddress` property and `ChangeEmailAddress` method from `Team` entity
- [x] 1.2 Remove `emailAddress` parameter from `Team.Create` factory method
- [x] 1.3 Remove `CreateTeamCommand.EmailAddress` and update `CreateTeamHandler` accordingly
- [x] 1.4 Remove `UpdateTeamCommand.EmailAddress` and update `UpdateTeamHandler` accordingly
- [x] 1.5 Generate EF Core migration to drop the `Email` column from the `Teams` table

## 2. API — Remove Email from Request/Response Contracts

- [x] 2.1 Remove `Email` field from `CreateTeamHttpRequest` and its `CreateTeamValidator`
- [x] 2.2 Remove `Email` field from `UpdateTeamHttpRequest` and its `UpdateTeamValidator`
- [x] 2.3 Remove `EmailAddress` field from `TeamDto` (GET team details response)
- [x] 2.4 Run ArchTests to verify no architectural violations introduced

## 3. Admin UI — Regenerate SDK and Remove Email from Forms

- [x] 3.1 Start Aspire (`aspire start --isolated`), wait for API, fetch `/openapi/v1.json`, run `pnpm openapi-ts` to regenerate the SDK
- [x] 3.2 Remove the email input field from the Create Team form
- [x] 3.3 Remove the email input field from the Team Settings form
- [x] 3.4 Remove server-side validation error display for email in both forms

## 4. Admin UI — SSR for Team Settings Layout

- [x] 4.1 Convert `teams/[teamId]/settings/layout.tsx` to an async Server Component (remove `"use client"`, receive `params` as a prop, fetch team details server-side)
- [x] 4.2 Extract the active-link sidebar nav into a `NavLinks` Client Component (uses `usePathname()`) and import it from the layout
- [x] 4.3 Verify the team name appears immediately in breadcrumbs and heading on hard refresh (no GUID flash)

## 5. Admin UI — SSR for Event Settings Layout

- [x] 5.1 Convert `teams/[teamId]/events/[eventId]/settings/layout.tsx` to an async Server Component (remove `"use client"`, receive `params` as a prop, fetch both team details and event details server-side)
- [x] 5.2 Extract the active-link sidebar nav into a `NavLinks` Client Component and import it from the layout
- [x] 5.3 Verify both team name and event name appear immediately in breadcrumbs and heading on hard refresh (no GUID flash)

## 6. Tests — Update Existing Tests

- [x] 6.1 Update `CreateTeam` domain/integration tests: remove email from test fixtures and builders
- [x] 6.2 Update `UpdateTeam` domain/integration tests: remove email from test fixtures
- [x] 6.3 Update `GetTeam` integration/API tests: remove email field assertions from response checks
- [x] 6.4 Run all test suites (`ArchTests`, `DomainTests`, `IntegrationTests`, `Api.Tests`) and confirm green
