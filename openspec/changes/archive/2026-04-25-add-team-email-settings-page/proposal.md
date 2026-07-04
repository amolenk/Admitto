## Why

The Email module already supports team-scoped email settings (admin API endpoints exist at `/admin/teams/{teamSlug}/email-settings`), but the Admin UI has no surface for managing them — organizers can only configure email per event today. This forces every event to repeat the same SMTP/from-address values and prevents teams from setting a single fallback that all events inherit. In addition, on the event Email tab there is no indication that team-scoped settings already exist, so organizers can't tell whether configuring an event will *override* a working team-level fallback or fill an absence.

## What Changes

- Add a new **Email** entry to the team settings sidebar (alongside General, Members, Danger zone) routed at `/teams/{teamSlug}/settings/email`, presenting a form for the team-scoped `EmailSettings` aggregate (SMTP host, port, from-address, auth mode, credentials).
- Reuse the existing event-tab email form structure for the team page (same fields, same secret-masking semantics, same optimistic-concurrency `Version` flow), wired to the team-scoped admin endpoints.
- Allow organizers to delete team-scoped settings from the team Email page (`DELETE` against the team-scope endpoint), with a confirmation step.
- Update the **event** Email tab to clearly indicate whether team-scoped settings exist for the owning team:
  - When team-scoped settings exist AND the event has no event-scoped row, show a callout: "Inherited from team settings" with a link to the team Email page; the event form starts empty/unconfigured but is editable to override.
  - When team-scoped settings exist AND the event has its own row, show a callout: "Overriding team settings" so the organizer knows the event row supersedes the team fallback.
  - When team-scoped settings do NOT exist, behaviour is unchanged.
- No backend changes — all admin endpoints, the `EmailSettings` aggregate, and the effective-settings resolver are already in place.

## Capabilities

### New Capabilities
- `admin-ui-team-email-settings`: Admin UI page for configuring, updating, and deleting team-scoped email server settings, mirroring the event Email tab and following the existing team-settings tabbed layout.

### Modified Capabilities
- `admin-ui-event-management`: The event Email tab requirement is extended to surface a team-level inheritance indicator so organizers can tell when team-scoped settings exist (inherited or overridden) versus when no fallback is in place.

## Impact

- **Admin UI** (`src/Admitto.UI.Admin`): new route segment `app/(dashboard)/teams/[teamSlug]/settings/email/`, new sidebar nav item in the team settings layout, a new query against the team-scoped email-settings endpoint on the event Email page, and a Next.js API route proxy at `app/api/teams/[teamSlug]/email-settings/route.ts` matching the existing event-scoped proxy.
- **Existing event Email tab**: gains a fetch for team-scoped settings (404-tolerant) and a callout block above/below the form. Save/delete behaviour is unchanged.
- **Backend**: no changes. The team-scoped admin endpoints (`GET/PUT/DELETE /admin/teams/{teamSlug}/email-settings`) and `IEventEmailFacade` resolution rules are already implemented.
- **Specs**: new `admin-ui-team-email-settings` capability, modified `admin-ui-event-management` Email tab requirement.
