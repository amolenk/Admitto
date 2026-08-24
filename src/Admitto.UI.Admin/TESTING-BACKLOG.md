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

Counts across the 199 scenario rows: `TODO` 141, `TESTED` 45, `DRIFT` 9, `BROWSER` 4, `GONE` 4,
`BACKEND` 3. Seven rows carry two classes (a tested scenario that also diverges from the spec),
so the tags total 206.

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
| View waitlist page for a ticket type with entries | `TODO` | `events/[eventId]/ticket-types/[ticketTypeId]/waitlist/page.tsx`. Includes email masking (`ali***@example.com`) and the claim-time countdown — both pure client logic, good test targets. |
| View waitlist page with no entries | `TODO` | Empty state. |
| Remove an active waitlist entry | `TODO` | The UI half: DELETE + refresh. Position shifting is `BACKEND`. |
| Remove entry triggers WaitlistMode re-evaluation | `BACKEND` | Domain rule; no UI surface. |
| WaitlistEnabled toggle appears only when capacity is configured | `TESTED` | `events/[eventId]/ticket-types/add-ticket-type-form.test.tsx` covers the add form's cascade; the edit form is `TODO`. |
| Organizer sets quiet hours on event | `TODO` (form) + `BACKEND` (coupon expiry) | `events/[eventId]/settings/waitlist/waitlist-policy-form.tsx`. |

## admin-ui-registrations (27)

| Scenario | Class | Target / note |
| :-- | :-- | :-- |
| Successfully add a registration | `TODO` | `events/[eventId]/registrations/add-registration-sheet.tsx`. |
| Client-side validation — missing first name | `TODO` | |
| Client-side validation — missing last name | `TODO` | |
| Client-side validation — missing email | `TODO` | |
| Server validation — duplicate email | `TODO` | Field-level mapping via `FormError`. |
| Server validation — event not active | `TODO` | General banner. |
| Ticket selection sourced from the event's catalog | `TODO` | Cancelled types not selectable. |
| Additional details rendered from the event schema | `TODO` | Dynamic fields + `maxLength` from `AdditionalDetailSchema`. High value: schema-driven rendering. |
| SC001 Page loads and shows registrations | `TODO` | `events/[eventId]/registrations/page.tsx`. |
| SC002 Empty event shows an empty-state row | `TODO` | |
| SC003 Attendee column shows first and last name | `TODO` | |
| SC004 Ticket column shows one badge per ticket | `TODO` | |
| SC005 Status column reflects the status | `TODO` | |
| SC006 Reconfirm column reflects HasReconfirmed | `TODO` | Event-zone formatting — leans on the tested `lib/time-zones.ts`. |
| SC007 Summary shows total only when capacity unset | `TODO` | Client-side arithmetic. |
| SC008 Summary shows total of capacity | `TODO` | |
| SC009 Search filters across name and email | `TODO` | `data-table*.tsx` family. |
| SC010 Ticket-type filter narrows rows | `TODO` | |
| SC011 Default sort is attendee name ascending | `TODO` | |
| SC012 Column header toggles sort direction | `TODO` | |
| SC013 Pagination shows 25 rows per page | `TODO` | |
| SC014 Next page advances the window | `TODO` | |
| SC015 Add registration navigates to the add page | `TODO` | |
| SC016 Export CSV shows a Coming soon notification | `DRIFT` | Export is implemented and downloads — see D4. |
| SC017 No multi-select checkbox column | `TODO` | A negative assertion; cheap to keep. |
| SC018 No status tabs above the table | `TODO` | |
| SC023 Clicking a row navigates to attendee detail | `TODO` | |

## admin-ui-attendee-detail (13)

Target: `events/[eventId]/registrations/[registrationId]/page.tsx` (client component, ~650 lines).

| Scenario | Class | Note |
| :-- | :-- | :-- |
| SC001 Renders details for a registered attendee | `TODO` | Name fallback to the email prefix is worth its own case. |
| SC002 Renders additional details | `TODO` | |
| SC003 Hides additional details when empty | `TODO` | |
| SC004 Timeline shows registration/reconfirmation milestones | `TODO` | Most-recent-first ordering is pure client logic. |
| SC005 Timeline shows cancellation with reason | `TODO` | Also asserts the Cancel button disappears. |
| SC006 Feed interleaves emails with activity | `TODO` | The merge of two `useQuery` results — the highest-value logic on the page. |
| SC006b Emails tab filters to emails only | `TODO` | |
| SC007 Empty state when no entries | `TODO` | |
| SC008 Loading state shows skeletons | `TODO` | |
| SC009 Cancel opens a dialog with reason selection | `TODO` | Asserts no request before confirming; reasons limited to `AttendeeRequest` / `VisaLetterDenied`. |
| SC009b Cancel confirmed calls the endpoint | `TODO` | |
| SC010 Change ticket types shows "Coming soon" | `DRIFT` | Implemented — see D5. |
| SC011 Back link returns to the registrations list | `TODO` | Assert the `href`. |

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
| Configure the registration window | `TODO` | `settings/registration/registration-policy-form.tsx`. |
| Configure an email-domain restriction | `TODO` | UI half only; enforcement is `BACKEND`. |
| No Open/Close controls on the page | `TODO` | Negative assertion. |
| Form is read-only for archived events | `TODO` | Disabled fields + banner (`settings/event-status-banner.tsx`). |
| Concurrency conflict surfaces to the user | `TODO` | |
| Add a new additional detail field | `TODO` | `settings/registration/additional-details-editor.tsx`. Key auto-generated as kebab-case — pure logic, high value. |
| Override the auto-generated key before persisting | `TODO` | |
| Reorder fields | `TODO` | Drag-and-drop; assert via keyboard/reorder handlers if exposed, else `BROWSER`. |
| Rename a field without changing its key | `TODO` | Persisted keys are read-only. |
| Remove a field requires confirmation | `TODO` | Confirmation copy about preserved historical values. |
| Editor is read-only for archived events | `TODO` | |
| Concurrency conflict (additional details) | `TODO` | |
| Configure the reconfirm policy without auto-cancel | `TODO` | `settings/reconfirm/reconfirm-policy-form.tsx`. |
| Configure the reconfirm policy with auto-cancel | `TODO` | |
| Max attempts hidden when auto-cancel is off | `TODO` | Same conditional-field pattern already tested on the ticket-type form. |
| Max attempts appears when auto-cancel is on | `TODO` | |
| Remove the reconfirm policy | `TODO` | |
| Validation error — close before open | `TODO` | |
| Validation error — non-positive cadence | `TODO` | |
| Validation error — non-positive minimum email interval | `TODO` | |
| Validation error — max attempts required when auto-cancel on | `TODO` | |
| Validation error — non-positive max attempts | `TODO` | |
| Reconfirm window opens at local 09:00 in event zone | `TESTED` | The conversion itself is covered by `lib/time-zones.test.ts` (incl. a DST boundary); wiring it into this form is `TODO`. |
| "Close after open" validation message uses event zone | `TODO` | |
| Navigate to policy pages | `TODO` | `events/[eventId]/edit/layout.tsx` tab bar. |
| Event header shows status | `TODO` | |
| Configure waitlist quiet hours | `TODO` | `settings/waitlist/waitlist-policy-form.tsx`. |
| General settings does not show waitlist quiet hours | `TODO` | Negative assertion on `settings/general-settings-form.tsx`. |
| Waitlist policy copy explains notification behavior | `TODO` | Copy assertion — low value, keep only as part of a render test. |
| Waitlist policy form is read-only for archived events | `TODO` | |
| Concurrency conflict (waitlist policy) | `TODO` | |

## admin-ui-event-management (59)

| Scenario | Class | Target / note |
| :-- | :-- | :-- |
| Hero card shows metadata without action buttons | `TODO` | `events/[eventId]/components/event-hero-card.tsx`. |
| Check-in card shows scanner without share link | `TODO` | `events/[eventId]/components/check-in-card.tsx`. |
| Successfully create an event (async) | `TODO` + `DRIFT` | `events/new/create-event-form.tsx`. Lands on the event dashboard, not `/edit/general` — see D7. |
| Duplicate public slug rejection is shown | `TODO` | |
| Display client-side validation error on create | `TODO` | |
| Create event option is hidden for non-owners | `TODO` | `components/nav-events.tsx` gates on `canCreateEvents`. |
| Display rejection from polling | `TODO` | The polling loop is the most intricate logic in the app — `Pending`/`Created`/`Rejected`/`Expired`, plus a deliberate 404 tolerance right after `202`. Needs fake timers. |
| Spinner shown while polling | `TODO` | Form disabled + `Progress` value. |
| Expired creation displays a timeout error | `TODO` | |
| Edit Event page is accessible from the sidebar | `TODO` | Assert the `href`. |
| Switching to Policies tab | `TODO` | `edit/layout.tsx`. |
| Switching to Danger zone tab | `TODO` | |
| Old settings URL redirects to General tab | `GONE` | No `settings/*` route and no redirect config — see N2. |
| Old settings/registration URL redirects | `GONE` | Same. |
| Old settings/reconfirm URL redirects | `GONE` | Same. |
| Post-creation redirect lands on General tab | `DRIFT` | See D7. |
| Successfully update event name | `TODO` | `settings/general-settings-form.tsx`. |
| Edit form shows current public slug | `TODO` | |
| Display concurrency conflict (General tab) | `TODO` | |
| Selected team accent variable is available | `TODO` | Low value; a `MAY` requirement. |
| Configure registration window (Registration tab) | `TODO` | Duplicate of the event-policies scenario. |
| Add a ticket type | `TESTED` | `ticket-types/add-ticket-type-form.test.tsx`. |
| Registration status defaults to Draft | `TODO` | |
| Add ticket type with self-service and capacity limit | `TESTED` | |
| Add ticket type with self-service disabled | `TODO` | The toggle exists and defaults on; the off-path is untested. |
| Add ticket type with unlimited self-service capacity | `TESTED` | |
| Remove capacity limit on existing ticket type | `TODO` | `ticket-types/edit-ticket-type-form.tsx` — the "can't clear a capacity" fix this requirement exists for. Priority. |
| Self-service indicator shown in list | `TODO` | `ticket-types/page.tsx`. |
| Add a ticket type with two time slots | `TESTED` | |
| Add a ticket type with no time slots | `TESTED` | Asserts `[]`, not `null`. |
| Reject invalid time-slot token | `TESTED` | |
| Suggestions are drawn from existing ticket types | `TESTED` | |
| No suggestions when event has no time slots | `TESTED` | Implicit: the default render has none. |
| Card shows time slots | `TODO` | `ticket-types/page.tsx`. |
| Card omits the row when no time slots | `TODO` | |
| Time slots visible but not editable (edit dialog) | `TODO` | Also: the edit payload must omit `timeSlots`. |
| Edit dialog hides the section when no time slots | `TODO` | |
| Header shows event name | `TODO` | |
| Header falls back to slug while loading | `TODO` | |
| Header summary uses "registered" | `TODO` | Verified present in the source. |
| Card stat label uses "Registered" | `TODO` | Verified present. |
| Active, in-stock ticket type shows "Available" | `TODO` | Verified present. |
| No footer action bar | `TODO` | Negative assertion. |
| Overflow menu shows only Edit | `TODO` | |
| Card hides slug | `TODO` | Negative assertion. |
| Card shows perforated divider | `BROWSER` | Pure CSS treatment. |
| No layout shift versus prior card | `BROWSER` | Visual regression. |
| Create form requires time zone | `TODO` | Defaults to the browser zone; explicit confirmation required. |
| General tab edits the time zone | `TODO` | |
| Unknown IANA zone rejected | `TODO` | `isValidTimeZone` is already tested; the inline surfacing is `TODO`. |
| Picker writes wall-clock time in event zone | `TESTED` | `lib/time-zones.test.ts` (`wallClockToUtcIso`, incl. DST). Wiring into `ui/zoned-date-time-picker.tsx` is `TODO`. |
| Picker reads UTC and shows local | `TESTED` | `utcIsoToWallClock` + round-trip tests. |
| Zone label displayed on every picker | `TODO` | `formatZoneCaption` is tested; its presence on each picker is not. |
| Emails sidebar entry links to bulk emails list | `TODO` | `components/nav-events.tsx`. |
| Emails entry is active on the list page | `TODO` | |
| Emails entry is NOT active on the event edit page | `TODO` | The `nav-links.test.tsx` prefix-matching pattern applies here too. |
| Emails entry is active on the detail page | `TODO` | |
| Archived events are not shown on the events list | `TODO` | Verify whether filtering is client- or server-side; if server-side, this is `BACKEND`. |
| Archived event disappears immediately after archive | `TODO` | Query invalidation. |

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
