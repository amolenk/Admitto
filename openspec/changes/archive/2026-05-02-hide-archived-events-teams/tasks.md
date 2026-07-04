## 1. Backend – Filter archived events from the listing query

- [x] 1.1 In `GetTicketedEventsHandler`, add `.Where(e => e.Status != EventLifecycleStatus.Archived)` to exclude archived events from the query result
- [x] 1.2 Update the XML doc comment on `GetTicketedEventsHttpEndpoint` to remove the mention of "archived" from the returned statuses

## 2. Tests – Backend listing excludes archived

- [x] 2.1 In `Admitto.Module.Registrations.Tests`, add a test for `GetTicketedEventsHandler` verifying that an archived event is not returned when listing events for a team (scenario `SC001_ListActiveEventsExcludesArchived`)
- [x] 2.2 In `Admitto.Api.Tests`, add an end-to-end test for `GET /admin/teams/{teamSlug}/events` verifying that an archived event is absent from the response (scenario `SC002_AdminListingExcludesArchivedEvents`)

## 3. UI – Confirm immediate disappearance after archive

- [x] 3.1 Verify that after a successful archive action in the event Danger Zone page the `queryClient.invalidateQueries({ queryKey: ["events", teamSlug] })` call already causes the event to disappear from the sidebar nav (nav-events.tsx already applies a client-side `status !== "archived"` filter — no code change needed if backend fix plus existing cache invalidation is sufficient)
- [x] 3.2 If the events list in the sidebar still briefly shows the archived event between invalidation and refetch (stale-while-revalidate window), update `handleArchive` in `danger/page.tsx` to optimistically remove the event from the `["events", teamSlug]` cache via `queryClient.setQueryData` before navigating
