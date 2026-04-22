## 1. Backend — GetRegistrations use case

- [x] 1.1 Add `RegistrationListItemDto` (Id, Email, Tickets[{Slug,Name}], CreatedAt) under `Application/UseCases/Registrations/GetRegistrations/`.
- [x] 1.2 Implement `GetRegistrationsQuery` and `GetRegistrationsHandler` returning `IReadOnlyList<RegistrationListItemDto>` for an event id, ordered by `CreatedAt` desc.
- [x] 1.3 Verify `Registration` exposes a creation timestamp; if missing, derive from existing audit columns or add an EF-tracked `CreatedAt` (no domain event change).
- [x] 1.4 Use `IOrganizationScopeResolver` to resolve `teamSlug`+`eventSlug` → `TicketedEventId`; return not-found result when either slug is unknown.

## 2. Backend — Admin HTTP endpoint

- [x] 2.1 Add `GetRegistrationsHttpEndpoint` mapping `GET /admin/teams/{teamSlug}/events/{eventSlug}/registrations` with `RequireTeamMembership(Organizer)`.
- [x] 2.2 Wire endpoint into `RegistrationsModule.MapRegistrationsAdminEndpoints`.
- [x] 2.3 Mirror the OpenAPI summary/description style of `AdminRegisterAttendeeHttpEndpoint`.

## 3. Backend tests

- [x] 3.1 Application tests in `Admitto.Module.Registrations.Tests` covering SC001–SC004 (empty list, multi-row, multi-ticket per row, event isolation).
- [x] 3.2 API endpoint tests in `Admitto.Api.Tests` covering SC005 (unknown team), SC006 (unknown event), SC007 (organizer 200), SC008 (non-member 403).
- [x] 3.3 Run module + API test suites; all green.

## 4. CLI parity

- [x] 4.1 Regenerate `src/Admitto.Cli/ApiClient.g.cs` via `bash generate-api-client.sh` (requires API running via Aspire).
- [x] 4.2 Add `ListRegistrationsCommand` under `src/Admitto.Cli/Commands/Events/Registration/` with `--team` and `--event` options.
- [x] 4.3 Wire the command into `src/Admitto.Cli/Program.cs` under `event → registration` next to `add`.
- [ ] 4.4 Smoke-test `admitto event registration list --team … --event …` against a running stack (operator task).

## 5. UI SDK & BFF

- [x] 5.1 Regenerate the Admin UI SDK (`curl /openapi/v1.json && pnpm openapi-ts`) and verify no other drift introduced.
- [x] 5.2 Add BFF route `app/api/teams/[teamSlug]/events/[eventSlug]/registrations/route.ts` GET handler that calls `getRegistrations` via `callAdmittoApi` (preserve the existing POST handler for add).

## 6. UI page

- [x] 6.1 Create `app/(dashboard)/teams/[teamSlug]/events/[eventSlug]/registrations/page.tsx` as a client component (table state lives in the page).
- [x] 6.2 Fetch registrations via `useQuery` + `apiClient.get` (full payload) and event details via the existing event query for ticket types & capacity.
- [x] 6.3 Render the summary tile per SC007/SC008: `Total: <N>` or `Total: <N> of <capacity>` (sum of `MaxCapacity` across ticket types when non-zero).
- [x] 6.4 Render the table (Attendee, Ticket, Status, Reconfirm, Registered) with badge styling matching existing pages; hardcode Status="Confirmed" and Reconfirm="—".
- [x] 6.5 Implement client-side search (email substring), ticket-type filter (select), column sort (default Registered desc; sortable Attendee/Ticket/Registered), and pagination (25/page).
- [x] 6.6 Add primary "Add registration" button linking to `./registrations/add`; add secondary "Export CSV" button that opens a "Coming soon" notification/dialog.
- [x] 6.7 Render an empty-state message when the unfiltered list is empty (SC002).
- [x] 6.8 Ensure no multi-select, no Company column, no status tabs, no "Confirmed/Pending" tiles (SC017/SC018).

## 7. Verification

- [x] 7.1 `cd src/Admitto.UI.Admin && pnpm build` is clean.
- [x] 7.2 Run targeted backend test suites for changes (Module.Registrations, Api).
- [x] 7.3 `openspec validate admin-ui-registrations-list --strict` passes.
- [ ] 7.4 Manual UI sanity check: load the page on an event with multiple registrations & ticket types; confirm sort/search/filter/page work and Export CSV shows the Coming-soon notification.
