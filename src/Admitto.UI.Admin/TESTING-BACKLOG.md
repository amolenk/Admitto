# Admin UI testing backlog

This file is the harvest of the ten `admin-ui-*` capabilities in `openspec/specs/`, which
Phase 3 of `OPENSPEC_TO_TESTS_PLAN.md` deletes. All **199 scenarios / 60 requirements** are
recorded below so that nothing is lost when the specs go, whether or not a test exists yet.

Per the migration plan, **code is the source of truth**. Where a spec contradicts the
implementation the code wins and the divergence is recorded under [Drift](#drift) rather than
silently encoded in a test.

## How to read this

| Class | Meaning |
| :-- | :-- |
| `TESTED` | A test exists today. The target file is named. |
| `TODO` | Testable in jsdom (Vitest + RTL). Not written yet; target file named. |
| `BROWSER` | Needs a real browser or a running server — deferred to the Playwright increment. |
| `BACKEND` | The behaviour lives in a .NET suite. A UI test would assert the mock, not the rule. |
| `DRIFT` | Spec contradicts the code. Resolved in favour of the code — see [Drift](#drift). |
| `GONE` | The spec describes something that is not implemented at all. See [Not implemented](#not-implemented). |

Counts across the 199 scenario rows: `TODO` 0, `TESTED` 176, `DRIFT` 12, `BROWSER` 4, `GONE` 17,
`BACKEND` 5. Fifteen rows carry two classes (a tested scenario that also diverges from the spec),
so the tags total 214.

Paths below are relative to `app/(dashboard)/teams/[teamId]/`.

---

## admin-ui-team-danger-zone (6)

Target: `settings/danger/page.tsx` — tests in `settings/danger/page.test.tsx`.

| Scenario | Class | Note |
| :-- | :-- | :-- |
| Successfully archive a team | `TESTED` | Also asserts `expectedVersion` and the cleared switcher selection. |
| Cancel archive action | `TESTED` | Plus an extra: the typed confirmation is cleared on reopen. |
| Reject archive with incorrect confirmation | `TESTED` + `DRIFT` | Confirm stays disabled. Matches on team **name**, not slug — see D1. |
| Display error when team has active events | `TESTED` | Backend's `detail` is surfaced verbatim. |
| Display concurrency conflict error on archive | `TESTED` | Same path as above. |
| Danger Zone tab is accessible from settings navigation | `TESTED` | `settings/nav-links.test.tsx`. |

## admin-ui-team-membership (10)

Target: `settings/members/page.tsx` — tests in `settings/members/page.test.tsx`.

| Scenario | Class | Note |
| :-- | :-- | :-- |
| View members list | `TESTED` | Email + role per row. |
| View empty members list | `TESTED` | "No members yet". |
| Members tab is accessible from settings navigation | `TESTED` | `settings/nav-links.test.tsx`. |
| Successfully add a team member | `TESTED` | Posts `{email, role}`; form clears. Extra test covers a non-default role. |
| Display error when adding duplicate member | `TESTED` | |
| Display validation error for invalid email | `TESTED` + `DRIFT` | Implemented as a disabled button, not a field error — see D2. |
| Successfully change a member's role | `TESTED` | PUT fires on selection, no save step. |
| Display error on role change failure | `TESTED` | Role "reverts" because the dropdown is server-driven, not optimistic. |
| Successfully remove a team member | `TESTED` | |
| Cancel member removal | `TESTED` | |

## admin-ui-team-api-keys (11)

Target: `settings/api-keys/page.tsx` — tests in `settings/api-keys/page.test.tsx`.

| Scenario | Class | Note |
| :-- | :-- | :-- |
| SC101 API Keys nav entry is visible | `TESTED` | `settings/nav-links.test.tsx`. |
| SC102 Clicking API Keys navigates | `TESTED` | Asserted as the link `href`; real navigation is `BROWSER`. |
| SC103 List shows all keys | `TESTED` | Name, prefix, creator, created date, status. |
| SC104 Active badge / revoked date | `TESTED` | |
| SC105 Empty state when no keys | `TESTED` | |
| SC106 Successful creation shows raw key once | `TESTED` | Plus the clipboard copy and a "prefix only in the list" test. |
| SC107 Dismissing the dialog adds the key | `TESTED` | Raw key leaves the DOM; name field resets. |
| SC108 Validation error when name is empty | `TESTED` + `DRIFT` | Disabled button, not a field error — see D2. |
| SC109 Revoke only for active keys | `TESTED` | |
| SC110 Confirmation before revoking | `TESTED` | Asserts no request is sent before confirming. |
| SC111 Confirmed revocation updates the list | `TESTED` | DELETE asserted; the list refresh is a query invalidation. |

## admin-ui-team-crud (13)

| Scenario | Class | Target / note |
| :-- | :-- | :-- |
| Successfully create a team | `TESTED` + `DRIFT` | `teams/add/create-team-form.test.tsx`. Redirects to `/settings`, not `/events` — see D3. |
| Display validation errors on create | `TESTED` | `teams/add/create-team-form.test.tsx`. Empty name blocks the request. |
| Navigate to create team from team switcher | `TESTED` | `components/team-switcher.test.tsx` — `router.push("/teams/add")`. |
| Successfully update team name | `TESTED` | `settings/team-settings-form.test.tsx`. |
| Successfully update team accent color | `TESTED` | `settings/team-settings-form.test.tsx`. |
| Display concurrency conflict error | `TESTED` | `settings/team-settings-form.test.tsx`. Partial update carries the loaded `version`. |
| Teams appear in alphabetical order | `BACKEND` | `GetTeamsHandler` does `OrderBy(t => t.Name)`; the switcher renders the received order. A UI test would only assert the mock's order — one cheap "preserves API order" test is the most that is worth writing. |
| Team name is present on initial render | `BROWSER` | `settings/layout.tsx` is an async server component; the no-GUID-flash claim is about streamed HTML. |
| Unauthenticated access is redirected | `BROWSER` | better-auth + Keycloak. |
| Navigate to team settings from sidebar | `TESTED` | `components/nav-settings.test.tsx` — asserts the `router.push` target (the entry is a button, not an anchor). |
| Settings entry is hidden for non-owners | `TESTED` | `components/nav-settings.test.tsx`. `nav-settings.tsx` returns null unless `canManageTeamSettings`. High value: a permissions rule expressed purely in the UI. |
| Switch team resets content | `TESTED` | `components/team-switcher.test.tsx`. `team-switcher.tsx` pushes `/` only when the id actually changes. |
| First team selection does not navigate | `TESTED` | Covered from the other side by `hooks/use-teams.test.ts` (auto-select does not push). |

## admin-ui-waitlist (6)

| Scenario | Class | Target / note |
| :-- | :-- | :-- |
| View waitlist page for a ticket type with entries | `TESTED` | `events/[eventId]/ticket-types/[ticketTypeId]/waitlist/page.test.tsx` covers masked email, countdown, ranked rows, and stats. |
| View waitlist page with no entries | `TESTED` | Empty state covered in the waitlist page test. |
| Remove an active waitlist entry | `TESTED` | DELETE + refresh covered; position shifting is `BACKEND`. |
| Remove entry triggers WaitlistMode re-evaluation | `BACKEND` | Domain rule; no UI surface. |
| WaitlistEnabled toggle appears only when capacity is configured | `TESTED` | Add and edit form cascades are covered by their colocated tests. |
| Organizer sets quiet hours on event | `TESTED` (form) + `BACKEND` (coupon expiry) | `events/[eventId]/settings/waitlist/waitlist-policy-form.test.tsx`. |

## admin-ui-registrations (27)

| Scenario | Class | Target / note |
| :-- | :-- | :-- |
| Successfully add a registration | `TESTED` | `events/[eventId]/registrations/add-registration-sheet.test.tsx` and the stateful page test cover submission and refresh. |
| Client-side validation — missing first name | `TESTED` | |
| Client-side validation — missing last name | `TESTED` | |
| Client-side validation — missing email | `TESTED` | |
| Server validation — duplicate email | `TESTED` | The actual general 409 error is asserted. |
| Server validation — event not active | `TESTED` | General banner. |
| Ticket selection sourced from the event's catalog | `TESTED` + `BACKEND` | UI renders the catalog response; active/cancelled availability is an API contract because `TicketTypeDto` has no lifecycle field. |
| Additional details rendered from the event schema | `TESTED` | Dynamic fields + `maxLength` from `AdditionalDetailSchema`. |
| SC001 Page loads and shows registrations | `TESTED` | `events/[eventId]/registrations/page.test.tsx`. |
| SC002 Empty event shows an empty-state row | `TESTED` | |
| SC003 Attendee column shows first and last name | `TESTED` | |
| SC004 Ticket column shows one badge per ticket | `TESTED` | |
| SC005 Status column reflects the status | `TESTED` | |
| SC006 Reconfirm column reflects HasReconfirmed | `TESTED` + `DRIFT` | The current component formats in the host zone rather than the event zone. |
| SC007 Summary shows total only when capacity unset | `GONE` | The registrations page has no summary tile. |
| SC008 Summary shows total of capacity | `GONE` | The registrations page has no summary tile. |
| SC009 Search filters across name and email | `TESTED` | |
| SC010 Ticket-type filter narrows rows | `TESTED` | |
| SC011 Default sort is attendee name ascending | `TESTED` | |
| SC012 Column header toggles sort direction | `TESTED` | |
| SC013 Pagination shows 25 rows per page | `TESTED` | |
| SC014 Next page advances the window | `TESTED` | |
| SC015 Add registration navigates to the add page | `TESTED` + `DRIFT` | The implemented affordance opens an add-registration sheet. |
| SC016 Export CSV shows a Coming soon notification | `TESTED` + `DRIFT` | Export is implemented and downloads — see D4. |
| SC017 No multi-select checkbox column | `TESTED` | |
| SC018 No status tabs above the table | `TESTED` | |
| SC023 Clicking a row navigates to attendee detail | `TESTED` | |

## admin-ui-attendee-detail (13)

Target: `events/[eventId]/registrations/[registrationId]/page.tsx` (client component, ~650 lines).

| Scenario | Class | Note |
| :-- | :-- | :-- |
| SC001 Renders details for a registered attendee | `TESTED` | Includes the email-prefix name fallback. |
| SC002 Renders additional details | `TESTED` | |
| SC003 Hides additional details when empty | `TESTED` | |
| SC004 Timeline shows registration/reconfirmation milestones | `TESTED` | |
| SC005 Timeline shows cancellation with reason | `TESTED` | |
| SC006 Feed interleaves emails with activity | `TESTED` | |
| SC006b Emails tab filters to emails only | `TESTED` | |
| SC007 Empty state when no entries | `TESTED` | |
| SC008 Loading state shows skeletons | `TESTED` | |
| SC009 Cancel opens a dialog with reason selection | `TESTED` | |
| SC009b Cancel confirmed calls the endpoint | `TESTED` | |
| SC010 Change ticket types shows "Coming soon" | `TESTED` + `DRIFT` | Implemented — see D5. |
| SC011 Back link returns to the registrations list | `TESTED` | |

## admin-ui-bulk-emails (23)

| Scenario | Class | Target / note |
| :-- | :-- | :-- |
| List page renders all jobs newest-first | `TESTED` | `events/[eventId]/emails/campaigns/page.test.tsx`. Asserted as order-preservation, since the component does not itself sort — same shape as D8. |
| Status filter narrows results | `TESTED` | `isActive`/`isCompleted`/`isFailedOrCancelled` exercised through the Select, one case each. |
| Empty state shown when no jobs exist | `TESTED` | |
| Row click navigates to job detail | `TESTED` | Full-page navigation via `window.location.href`, not `router.push`. |
| Old emails URL redirects to campaigns tab | `TESTED` + `DRIFT` | `emails/page.test.ts`. `redirect()` is a 307, not the specified 308 — see D6. |
| Detail page shows job summary | `TESTED` | `emails/campaigns/[jobId]/page.test.tsx`. |
| Detail page shows the attendee filter | `TESTED` | |
| Cancel button present for active jobs | `TESTED` | |
| Cancel button absent for terminal jobs | `TESTED` | |
| Cancel success refreshes status | `TESTED` | Asserted via the badge updating after `invalidateQueries` triggers a refetch. |
| Old job detail URL redirects to new path | `GONE` | No route or redirect for `/emails/{jobId}` — see N1. |
| Send bulk email opens as Sheet | `TESTED` | `emails/send-bulk-email-sheet.test.tsx`. |
| Sheet collects direct content | `TESTED` | Subject / text body / HTML body required. |
| Recipient selection is attendee-only | `TESTED` | Negative assertion: no file input. |
| Template selection is absent | `TESTED` | Negative assertion. |
| Sheet closes on successful submission | `TESTED` | |
| Navigating to /emails shows Campaigns by default | `TESTED` | `emails/page.test.ts`. |
| Campaigns remains available | `TESTED` | Subsumed by the list-page render test in `campaigns/page.test.tsx`. |
| Templates tab removed | `TESTED` | Negative assertion in `emails/layout.test.tsx`. |
| Setup tab removed | `TESTED` | Negative assertion in `emails/layout.test.tsx`. |
| List proxy forwards GET | `TESTED` | Covered generically by `lib/admitto-api/admitto-client.node.test.ts`; the routes are 3-line delegations. |
| Create proxy forwards direct content POST | `TESTED` | Same. |
| Cancel proxy forwards POST | `TESTED` | Same. |

## admin-ui-event-policies (31)

Targets under `events/[eventId]/settings/` (forms) rendered by `events/[eventId]/edit/policies/page.tsx`.

| Scenario | Class | Target / note |
| :-- | :-- | :-- |
| Configure the registration window | `TESTED` | `settings/registration/registration-policy-form.test.tsx`. |
| Configure an email-domain restriction | `TESTED` + `BACKEND` | UI form is covered; enforcement is backend-owned. |
| No Open/Close controls on the page | `TESTED` | |
| Form is read-only for archived events | `TESTED` | Disabled fields + banner. |
| Concurrency conflict surfaces to the user | `TESTED` | |
| Add a new additional detail field | `TESTED` | Kebab-case key generation is covered. |
| Override the auto-generated key before persisting | `TESTED` | |
| Reorder fields | `GONE` | The editor has no reorder control or handler. |
| Rename a field without changing its key | `TESTED` | |
| Remove a field requires confirmation | `TESTED` | Includes the persisted field-list assertion. |
| Editor is read-only for archived events | `TESTED` | |
| Concurrency conflict (additional details) | `TESTED` | |
| Configure the reconfirm policy without auto-cancel | `TESTED` | |
| Configure the reconfirm policy with auto-cancel | `GONE` | The policy contract and form expose no auto-cancel field. |
| Max attempts hidden when auto-cancel is off | `GONE` | The policy contract and form expose no max-attempt field. |
| Max attempts appears when auto-cancel is on | `GONE` | The policy contract and form expose no max-attempt field. |
| Remove the reconfirm policy | `TESTED` | |
| Validation error — close before open | `TESTED` | |
| Validation error — non-positive cadence | `TESTED` | |
| Validation error — non-positive minimum email interval | `TESTED` | |
| Validation error — max attempts required when auto-cancel on | `GONE` | The policy contract and form expose no max-attempt field. |
| Validation error — non-positive max attempts | `GONE` | The policy contract and form expose no max-attempt field. |
| Reconfirm window opens at local 09:00 in event zone | `TESTED` | Conversion and form wiring are covered. |
| "Close after open" validation message uses event zone | `TESTED` | |
| Navigate to policy pages | `TESTED` | |
| Event header shows status | `TESTED` + `DRIFT` | The status is rendered as a policy-page banner, not in the edit header. |
| Configure waitlist quiet hours | `TESTED` | |
| General settings does not show waitlist quiet hours | `TESTED` | |
| Waitlist policy copy explains notification behavior | `TESTED` | |
| Waitlist policy form is read-only for archived events | `TESTED` | |
| Concurrency conflict (waitlist policy) | `TESTED` | |

## admin-ui-event-management (59)

| Scenario | Class | Target / note |
| :-- | :-- | :-- |
| Hero card shows metadata without action buttons | `TESTED` | `components/event-cards.test.tsx`. |
| Check-in card shows scanner without share link | `TESTED` | |
| Successfully create an event (async) | `TESTED` + `DRIFT` | Lands on the event dashboard, not `/edit/general` — see D7. |
| Duplicate public slug rejection is shown | `TESTED` | Rejected polling outcome. |
| Display client-side validation error on create | `TESTED` | |
| Create event option is hidden for non-owners | `TESTED` | |
| Display rejection from polling | `TESTED` | Covers Pending/Created/Rejected/Expired and initial 404 tolerance with fake timers. |
| Spinner shown while polling | `TESTED` | |
| Expired creation displays a timeout error | `TESTED` | |
| Edit Event page is accessible from the sidebar | `TESTED` | |
| Switching to Policies tab | `TESTED` | |
| Switching to Danger zone tab | `TESTED` | |
| Old settings URL redirects to General tab | `GONE` | No `settings/*` route and no redirect config — see N2. |
| Old settings/registration URL redirects | `GONE` | Same. |
| Old settings/reconfirm URL redirects | `GONE` | Same. |
| Post-creation redirect lands on General tab | `TESTED` + `DRIFT` | See D7. |
| Successfully update event name | `TESTED` | `settings/general-settings-form.test.tsx`. |
| Edit form shows current public slug | `TESTED` | |
| Display concurrency conflict (General tab) | `TESTED` | |
| Selected team accent variable is available | `GONE` | General settings expose no accent control or consumer. |
| Configure registration window (Registration tab) | `TESTED` | Covered by the registration-policy form test. |
| Add a ticket type | `TESTED` | `ticket-types/add-ticket-type-form.test.tsx`. |
| Registration status defaults to Draft | `GONE` | Ticket-type UI has no registration-status control or Draft state. |
| Add ticket type with self-service and capacity limit | `TESTED` | |
| Add ticket type with self-service disabled | `TESTED` | |
| Add ticket type with unlimited self-service capacity | `TESTED` | |
| Remove capacity limit on existing ticket type | `TESTED` | |
| Self-service indicator shown in list | `GONE` | Ticket cards expose no self-service indicator. |
| Add a ticket type with two time slots | `TESTED` | |
| Add a ticket type with no time slots | `TESTED` | Asserts `[]`, not `null`. |
| Reject invalid time-slot token | `TESTED` | |
| Suggestions are drawn from existing ticket types | `TESTED` | |
| No suggestions when event has no time slots | `TESTED` | Implicit: the default render has none. |
| Card shows time slots | `TESTED` | |
| Card omits the row when no time slots | `TESTED` | |
| Time slots visible but not editable (edit dialog) | `TESTED` | The mutation payload omits `timeSlots`. |
| Edit dialog hides the section when no time slots | `TESTED` | |
| Header shows event name | `TESTED` | |
| Header falls back to slug while loading | `GONE` | Ticket types page has no loading-slug fallback. |
| Header summary uses "registered" | `TESTED` | |
| Card stat label uses "Registered" | `TESTED` | |
| Active, in-stock ticket type shows "Available" | `TESTED` | |
| No footer action bar | `TESTED` | |
| Overflow menu shows only Edit | `GONE` | Ticket cards have no overflow menu. |
| Card hides slug | `TESTED` | |
| Card shows perforated divider | `BROWSER` | Pure CSS treatment. |
| No layout shift versus prior card | `BROWSER` | Visual regression. |
| Create form requires time zone | `TESTED` | `events/new/create-event-form.test.tsx` selects the explicit Time zone field and asserts the chosen IANA zone is submitted. The field defaults to the browser zone and asks the organizer to confirm it. |
| General tab edits the time zone | `TESTED` | |
| Unknown IANA zone rejected | `TESTED` | |
| Picker writes wall-clock time in event zone | `TESTED` | Utility and component wiring are covered. |
| Picker reads UTC and shows local | `TESTED` | Utility and component wiring are covered. |
| Zone label displayed on every picker | `TESTED` | |
| Emails sidebar entry links to bulk emails list | `TESTED` | |
| Emails entry is active on the list page | `TESTED` | |
| Emails entry is NOT active on the event edit page | `TESTED` | |
| Emails entry is active on the detail page | `TESTED` | |
| Archived events are not shown on the events list | `TESTED` | |
| Archived event disappears immediately after archive | `TESTED` | Real archive flow covers query invalidation and removal. |

---

## Drift

Resolved in favour of the code, per the migration plan. Each of these is a spec statement that
no longer matches the implementation; none is a code change.

- **D1 — Danger zone confirmation matches the team *name*, not the slug.**
  `settings/danger/page.tsx:79` compares against `team.name`, and the dialog copy reads "type the
  team name below". `admin-ui-team-danger-zone` says slug. Tested as implemented.
- **D2 — Invalid input disables the submit button instead of showing a field error.**
  Both `settings/members/page.tsx` (invalid email) and `settings/api-keys/page.tsx` (blank name)
  gate the button rather than rendering a message. Three spec scenarios describe a validation
  error. The guard is equivalent in effect; the affordance differs. Tested as implemented.
- **D3 — Create team redirects to team settings, not the events page.**
  `teams/add/create-team-form.tsx:56` pushes `/teams/{teamId}/settings`;
  `admin-ui-team-crud` says `/teams/{teamId}/events`. Confirmed with the maintainer as a stale
  spec.
- **D4 — Registrations CSV export is implemented, not "coming soon".**
  `events/[eventId]/registrations/page.tsx:180` navigates to the export proxy route, which is
  real and now tested. `admin-ui-registrations` SC016 expects a "Coming soon" notification and
  no download. The spec predates `badge-export`.
- **D5 — "Change ticket types" is implemented, not a placeholder.**
  The attendee detail page has a working mutation with a success toast
  (`registrations/[registrationId]/page.tsx:321`). Two spec statements — SC010's "Coming soon"
  and the "Resend (no-op placeholder)" note — are both stale; resend is wired too (`:302`).
- **D6 — The `/emails` redirect is temporary (307), not permanent (308).**
  `emails/page.tsx` uses `redirect()`, whose default is temporary.
  `admin-ui-bulk-emails` says "permanently redirected". If the permanence matters, this is a
  one-word change to `permanentRedirect()` — flagged rather than assumed.
- **D7 — Event creation lands on the event dashboard, not the General tab.**
  `events/new/create-event-form.tsx:156` pushes `/teams/{teamId}/events/{eventId}`.
  `admin-ui-event-management` states `/edit/general` twice (in the create requirement and in
  "Post-creation redirect lands on General tab").
- **D8 — Alphabetical team ordering is a backend guarantee, not a UI one.**
  `admin-ui-team-crud` places the requirement on the team switcher, which does not sort;
  `GetTeamsHandler` does `OrderBy(t => t.Name)`. Case-insensitivity therefore depends on the
  database collation, not on JavaScript. The spec's example (`acme` before `Beta Corp`) is only
  guaranteed if the Postgres collation is case-insensitive — worth confirming separately.
- **D9 — Registration reconfirm timestamps use the host zone, not the event zone.**
  `registrations/page.tsx` calls `Date#toLocaleString` without a `timeZone` option.
- **D10 — Add registration opens a sheet, not a dedicated route.**
  `registrations/page.tsx` controls `AddRegistrationSheet` locally; no `/registrations/add` route
  is used.
- **D11 — Event status is a policy-page banner, not edit-header metadata.**
  `settings/event-status-banner.tsx` renders the archived warning; `edit/layout.tsx` only renders
  the event name and tabs.

## Not implemented

Spec statements with no corresponding code. Listed so the gap is visible after the specs are
deleted; each needs a keep-or-drop decision.

- **N1 — Old bulk-email detail URLs do not redirect.** `/emails/{jobId}` has no route, so an old
  link 404s instead of redirecting to `/emails/campaigns/{jobId}`.
- **N2 — Old `settings/*` event URLs do not redirect.** The pages moved to `edit/*`, but there is
  no `next.config.ts` `redirects` entry and no middleware, so all three specified 308s are
  absent. Old bookmarks 404.
- **N3 — The bare `/edit` path has no page.** `admin-ui-event-management` requires
  `/edit` → `/edit/general`; there is no `edit/page.tsx`, so `/edit` 404s. This is the one entry
  in this section that is plausibly a user-visible bug rather than dead spec text.
- **N4 — Registration summary tiles are absent.** The registrations page renders no total or
  capacity summary tile.
- **N5 — Reconfirmation auto-cancel and max-attempt settings are absent.** Neither the policy
  contract nor its form exposes those fields.
- **N6 — Several ticket-type controls are absent.** The UI has no Draft status, self-service
  card indicator, loading-slug fallback, or overflow menu.

## Out of scope for the jsdom suite

Recorded so these are not mistaken for gaps:

- The 54 vendored shadcn primitives in `app/components/ui/`, except the four hand-written ones
  (`multiple-selector.tsx`, `zoned-date-time-picker.tsx`, `date-time-picker.tsx`,
  `time-zone-selector.tsx`).
- `app/lib/admitto-api/generated/**` — regenerated from the OpenAPI spec.
- The 36 proxy routes that are plain `callAdmittoApi` delegations. The two hand-rolled CSV export
  routes are tested individually.
- The five server components (`(dashboard)/layout.tsx`, `(auth)/layout.tsx`,
  `(auth)/signin/page.tsx`, `settings/layout.tsx`) and the `api/auth/*` routes — Playwright.
- CSS and visual treatments (`BROWSER` rows above).
