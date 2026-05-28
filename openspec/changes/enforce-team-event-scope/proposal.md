## Why

All API endpoints that operate on events are routed under `/teams/{teamId}/events/{eventId}`, and `teamId` is validated for team membership by the auth layer. However, no handler (command or query) verifies that the `eventId` actually belongs to the claimed `teamId`. This means a user on Team A can construct requests using their own valid `teamId` combined with Team B's `eventId` and operate on the wrong event scope without being rejected.

## What Changes

- **Badges module**: Add `TeamId` to the `BadgesEvent` projection entity and database table (migration required). Propagate `TeamId` from `TicketedEventCreatedIntegrationEvent` through the create pipeline. Scope all badge command and query handlers to load `BadgesEvent` by both `eventId` and `teamId`.
- **Registrations module**: No schema migration needed (`TicketedEvent`, `Coupon`, and `Registration` already store `TeamId`). Update all command and query handlers that load event-scoped aggregates (`TicketedEvent`, `TicketCatalog`, `Coupon`, `Registration`) to include `teamId` in their load predicates or verify ownership at the parent level.
- **Email module**: No schema migration needed (`BulkEmailJob` already stores `TeamId`). Update `GetBulkEmailsHandler`, `GetBulkEmailHandler`, and `CancelBulkEmailHandler` to scope loads by both `teamId` and `eventId`/`bulkEmailJobId`. Fix `GetAttendeeEmailsHandler` where `TeamId` is passed in the query but silently ignored.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `event-management`: Team-event scope is now enforced on all event management commands and queries (archive, update details, update time zone, configure policies, etc.).
- `ticket-type-management`: Team-event scope is now enforced when loading ticket catalog and ticket types.
- `coupon-management`: Team-event scope is now enforced on all coupon commands and queries.
- `badge-type-management`: Team-event scope is now enforced on all badge type and badge instance commands and queries. `BadgesEvent` projection gains a `TeamId` field.
- `bulk-email`: Team-event scope is now enforced on bulk email list, detail, and cancel operations. `GetAttendeeEmails` bug fixed (TeamId was passed but unused).
- `attendee-emails`: `GetAttendeeEmails` now actually filters by `TeamId` (bug fix).

## Impact

- **Schema**: One new migration for `Admitto.Core` (Badges): adds `team_id` column to `badges_events` table.
- **Domain entities**: `BadgesEvent` gains a `TeamId` property.
- **Commands/queries**: `teamId` added to all affected command and query objects across Badges, Registrations, and Email modules.
- **Handlers**: Predicate changes in ~20 handlers across three modules.
- **Endpoints**: Pass `teamId` from route into commands/queries that previously ignored it.
- **No breaking API changes**: Route shapes and response contracts are unchanged.
