# Design: remove-reply-to-ui-and-sort-teams

## Context

Two small, independent Admin UI improvements bundled into one change:

1. The team reply-to email address (`Team.ReplyToEmailAddress`) is no longer consulted by the email sending pipeline. It is still present in the domain model, `UpdateTeam`/`GetTeam` API contracts, Email module projections, and the database. Full removal is deferred; for now only the Admin UI settings form field is removed. The only hand-written UI usage is in `src/Admitto.UI.Admin/app/(dashboard)/teams/[teamId]/settings/team-settings-form.tsx` (schema field, default value, submit logic, form markup).

2. The team switcher (`src/Admitto.UI.Admin/app/components/team-switcher.tsx`) renders teams in the order received from `GET /admin/teams`. `GetTeamsHandler.cs` applies no `OrderBy` in either its admin branch or its member branch, so ordering is undefined (effectively insertion order).

## Goals / Non-Goals

**Goals:**
- Remove the reply-to email field from the Team Settings form so users can no longer see or edit it in the UI.
- Return teams in alphabetical order (by name, case-insensitive) from the team list query so the team switcher shows them alphabetically.

**Non-Goals:**
- Removing `replyToEmailAddress` / `clearReplyToEmailAddress` from the API contract, domain model, integration events, Email projections, or database — deferred to a later cleanup change.
- Regenerating the Admin UI SDK (the OpenAPI contract does not change).
- Client-side sorting or sorting controls in the UI.

## Decisions

### D1: Sort in the backend query handler, not the UI

Add `OrderBy` on the team name to both branches of `GetTeamsHandler.cs`.

- **Why**: A single fix point covers every consumer (team switcher, CLI, future pages). It translates to SQL `ORDER BY`, avoiding a client-side sort in `use-teams.ts` or `team-switcher.tsx` that would have to be duplicated per consumer.
- **Alternative considered**: sorting in `use-teams.ts` after fetch — rejected because it only fixes one consumer and hides the undefined backend ordering.
- **Case-insensitivity / actual implementation**: `OrderBy(t => t.Name)` — ordering directly by the `TeamName` value object (not `t.Name.Value`). EF Core's Vogen-generated `TeamName.EfCoreValueConverter` translates a direct property key selector to SQL `ORDER BY` on the underlying column, but `t.Name.Value.ToLower()` (or even plain `t.Name.Value`) does **not** translate in an `OrderBy` key selector — EF throws `InvalidOperationException` ("could not be translated"), even though the identical `t.Name.Value` member access translates fine inside a `Select` projection. This is a known EF Core query-translation limitation specific to key selectors, not something worth working around with `AsEnumerable()`/client-side sorting for a simple list query.
- Both the local dev/test Postgres container (`postgres:18.3`, no explicit locale/collation configured) and the production Azure Postgres Flexible Server default to a locale-aware collation (not the byte-order `C`/`POSIX` collation), which sorts strings with case as a tie-breaker rather than a primary key. Handler-level tests with mixed-case names ("Zebra Events", "acme", "Beta Corp") confirm `OrderBy(t => t.Name)` alone yields the expected alphabetical order. If the deployment target's collation ever changes to a byte-order collation, this would need revisiting (e.g., a computed lower-case column or explicit collation on the column) — tracked as a risk below rather than solved preemptively.

### D2: UI-only removal of reply-to

Remove from `team-settings-form.tsx`: the `replyToEmailAddress` Zod schema field, the default value, the submit-time `replyToEmailAddress`/`clearReplyToEmailAddress` body assignments, and the `<FormField>` markup.

- **Why**: The API request type (`UpdateTeamHttpRequest`) treats reply-to as optional partial-update fields; omitting them means the stored value is simply left untouched. No API, SDK, or proxy change is required.
- **Alternative considered**: also clearing stored values on save — rejected; the stored value is inert and will be handled by the future cleanup change.

### D3: Spec deltas in two capabilities

- `admin-ui-team-crud`: modify the Team Settings form requirement (drop reply-to), remove the reply-to update scenario, and add an alphabetical-order requirement for the team switcher.
- `team-management`: modify the two list requirements (admin list, my-teams list) to mandate alphabetical ordering. The update/get requirements that mention reply-to stay unchanged because the API behavior is unchanged.

## Risks / Trade-offs

- [Stale reply-to values remain in the database and continue to flow through integration events and Email projections] → Acceptable; the sending pipeline no longer reads them, and the deferred cleanup change removes them end-to-end.
- [Users can still set reply-to via the raw API/CLI] → Acceptable for now; the field is documented as pending removal in the follow-up cleanup.
- [Plain `OrderBy(t => t.Name)` relies on the deployment's Postgres collation being locale-aware rather than byte-order (`C`); if that ever changes, mixed-case ordering could regress] → Covered by handler-level tests with mixed-case names; acceptable given both current dev/test and production Postgres targets use locale-aware default collation.

## Migration Plan

No migration needed. Both changes are behavior-only; no schema or contract changes. Rollback is a straight revert.

## Open Questions

None.
