## Context

The Admin UI currently shows all events (active, cancelled, **and archived**) because `GetTicketedEventsHandler` issues no status filter. The `GetTeamsHandler` already filters out archived teams correctly (`Where(t => t.ArchivedAt == null)`). The `event-management` spec already mandates that archived events be excluded from listings; the handler simply never enforced that rule.

On the UI side, when an organizer archives a team, the `admin-ui-team-danger-zone` flow already navigates away and refreshes the team list, so teams disappear immediately. No equivalent client-side cache invalidation exists for events: after archiving an event, the events list is only refreshed on the next full page load.

## Goals / Non-Goals

**Goals:**
- Exclude archived events from `GET /admin/teams/{teamSlug}/events` (single query filter change).
- After an archive-event action succeeds in the UI, remove the event from the local list immediately without a full page reload.
- Confirm (and document) that the teams listing already satisfies the requirement.

**Non-Goals:**
- Exposing archived events/teams via a dedicated endpoint — deferred to a future requirement.
- Filtering archived items from any public-facing endpoint (out of scope).
- Changing the archive mechanics themselves.

## Decisions

### Decision: Filter at query layer, not endpoint layer
Add `&& e.Status != EventLifecycleStatus.Archived` directly in `GetTicketedEventsHandler`. Keeping the filter at the query handler keeps the endpoint thin and the query self-documenting. An alternative of filtering at the endpoint was rejected because handlers should own their own contracts.

### Decision: No new query variant
A single `GetTicketedEventsQuery` with a hardcoded exclusion is simpler than a query with a `IncludeArchived` flag. When a future "view archived" endpoint is needed, a new, purpose-built query can be created.

### Decision: Optimistic UI removal on archive
After the archive API call succeeds, the events list page SHALL remove the newly archived event from local React state (or invalidate the list query cache), rather than waiting for a refresh. This matches the UX intent of "disappear immediately." The pattern already used for team archive (navigate + refresh) is the model to follow.

## Risks / Trade-offs

- **Risk**: Other callers of `GetTicketedEventsQuery` (e.g., future public endpoints) will also receive filtered results → **Mitigation**: No other callers exist today; when a separate archived-events endpoint is needed, a new query will be introduced, so existing callers are unaffected.
- **Trade-off**: Optimistic removal in UI means a network failure leaves the list stale until reload. Acceptable because the archive action is idempotent and a reload corrects it.
