## Context

The Email module already supports an `EmailSettings` aggregate keyed by `(Scope, ScopeId)` with `Scope ∈ {Team, Event}`, persisted in a single table, and admin endpoints exposed at:
- `GET/PUT/DELETE /admin/teams/{teamSlug}/email-settings` (team scope)
- `GET/PUT/DELETE /admin/teams/{teamSlug}/events/{eventSlug}/email-settings` (event scope)

Both endpoint families share one slice family parameterised by `(Scope, ScopeId)` (see `src/Admitto.Module.Email/Application/UseCases/EmailApiEndpoints.cs`). The `IEventEmailFacade` resolves effective settings as event-scoped → team-scoped → none, with no per-field merge.

The Admin UI (Next.js, `src/Admitto.UI.Admin`) already has:
- A team settings tabbed layout at `app/(dashboard)/teams/[teamSlug]/settings/layout.tsx` with three nav items: General, Members, Danger zone.
- An event Email tab at `app/(dashboard)/teams/[teamSlug]/events/[eventSlug]/settings/email/` with `page.tsx` and `email-settings-form.tsx`.
- A Next.js proxy route at `app/api/teams/[teamSlug]/events/[eventSlug]/email-settings/route.ts` that forwards to the backend.

What is missing on the UI side is (a) any team-scope page/route/proxy, and (b) any signal on the event page that team-scoped settings exist.

## Goals / Non-Goals

**Goals:**
- Provide a dedicated team Email settings page reachable from the team-settings sidebar, with the same affordances as the event Email tab (form, masking, optimistic-concurrency, delete).
- Make the relationship between team-scoped and event-scoped settings explicit on the event Email tab (inherited vs overriding vs neither).
- Maximise reuse: the team page and the event page should share the form component, differing only in scope wiring.

**Non-Goals:**
- No backend changes — the endpoints, persistence, encryption, and resolver are already implemented.
- No changes to email *templates* UI; this change is scoped to email *settings*.
- No introduction of per-field inheritance/merging — the current "all or nothing" override semantics in the resolver remain.
- No automatic copy-from-team affordance on the event page (deferred — see Open Questions).

## Decisions

### Decision 1: Reuse `EmailSettingsForm` as a scope-agnostic component

The existing `email-settings-form.tsx` is hard-coded to event slugs and the event API path. We will refactor it to take a `scope` discriminator plus an opaque endpoint path (or a small `{ getUrl, queryKey }` config), so both the team page and the event page mount the same component.

**Why:** The form UI is identical across scopes (same fields, same validation, same masking, same `Version` flow). Keeping two near-duplicate forms would drift over time. The slice family is already unified on the backend; the UI should mirror that.

**Alternative considered:** Duplicate the form for the team page. Rejected — a small refactor now is cheaper than the inevitable divergence.

### Decision 2: Team Email page lives under team settings, not as a top-level page

The new page is `app/(dashboard)/teams/[teamSlug]/settings/email/page.tsx`, with a fourth nav item "Email" added to `settings/layout.tsx`. This matches the existing tabbed pattern (General / Members / Danger zone) and reuses the same breadcrumbs and grid.

**Why:** Team settings are already tab-based; an unrelated top-level "team email" route would split where organizers look for team-wide configuration. The sidebar layout already accommodates a fourth item.

### Decision 3: Event page fetches both event-scoped and team-scoped settings to compute the indicator

On mount, the event Email tab fires two parallel `GET`s:
1. `/api/teams/{teamSlug}/events/{eventSlug}/email-settings` (existing)
2. `/api/teams/{teamSlug}/email-settings` (new proxy route, see Decision 4)

Both calls treat 404 as "no row" (already the existing pattern for the event call). The combination yields three UI states:

| event row | team row | UI shown                                                                 |
|-----------|----------|--------------------------------------------------------------------------|
| absent    | absent   | Empty form (current behaviour). No callout.                              |
| absent    | present  | Empty form + **"Inherited from team settings"** callout, link to team Email page. |
| present   | absent   | Pre-filled form (current behaviour). No callout.                         |
| present   | present  | Pre-filled form + **"Overriding team settings"** callout, link to team Email page. |

**Why:** The facade-side rule already says "event row, when present, fully overrides team row." Reflecting that at the UI keeps the mental model consistent with the resolver. We don't need a backend "is team configured?" query because the GET already returns DTO-or-404, and the lookup is cheap.

**Alternative considered:** Add a backend "effective email status" endpoint that returns `{ source: 'event' | 'team' | 'none', teamConfigured: bool }`. Rejected for now — it duplicates information already derivable from two GETs and adds API surface for a single screen.

### Decision 4: Add a Next.js proxy route for the team-scoped endpoint

We add `app/api/teams/[teamSlug]/email-settings/route.ts` matching the shape of the existing event-scoped proxy (`app/api/teams/[teamSlug]/events/[eventSlug]/email-settings/route.ts`). It forwards `GET`, `PUT`, and `DELETE` to the backend with the same auth-token plumbing.

**Why:** Consistent with the project's existing pattern of one Next.js API proxy per backend route family. No client-side direct-to-backend calls.

### Decision 5: Reuse the same `Version` optimistic-concurrency flow for delete

Delete on the team page sends `DELETE /admin/teams/{teamSlug}/email-settings` with the current `Version` in the request body (matching the event-scoped delete behaviour). A confirmation modal is required before issuing the request because the action is destructive.

**Why:** Mirrors the existing event-tab delete behaviour and the backend's existing concurrency contract.

## Risks / Trade-offs

- **[Risk] Two-call fetch on event page adds latency.** → Mitigation: both calls are issued in parallel via React Query; the team-scope GET is cheap (single row lookup). Skeleton is shown until both resolve.
- **[Risk] User confusion when overriding team settings unintentionally.** → Mitigation: the "Overriding team settings" callout names the team page and offers a link, so the relationship is discoverable. Saving an event-scoped row is still an explicit action.
- **[Risk] Team page accessible without team membership.** → Mitigation: backend already enforces team-membership authorization on the team-scoped endpoints (per `team-email-settings` and `email-settings` specs). The UI relies on the backend `403` and surfaces it via the standard form-error component.
- **[Trade-off] Refactoring `EmailSettingsForm` breaks no consumers but does touch the existing event tab.** → Mitigation: keep the visual output identical; the refactor is a prop-shape change only. Smoke-test the event tab after the change.

## Open Questions

- Should the event page offer a "Copy from team settings" button to pre-fill the event form when team-scoped settings exist? Deferred — out of scope for this change; would be a small follow-up.
- Should the team-settings sidebar use the icon `Mail` (matches the event sidebar's "Emails" item) or a more generic icon to keep team-settings nav visually distinct? Default: `Mail`. Open to feedback during implementation.
