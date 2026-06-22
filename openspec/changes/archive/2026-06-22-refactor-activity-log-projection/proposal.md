## Why

`ActivityLog` is specified as a read-side projection, but the current implementation models it as a domain entity behind a `WriteActivityLog` command slice. This makes derived timeline data look like command-side domain state and obscures the actual projection pattern.

## What Changes

- Move the persisted activity-log row type from `Registrations/Domain/Entities` to `Registrations/Application/Projections/ActivityLog/ActivityLogView.cs`.
- Rename the backing table from `activity_log` to `activity_log_view` to make the persisted read model explicit.
- Replace the `WriteActivityLog` command/handler slice with `Registrations/Application/Projections/ActivityLog/ActivityLogProjector.cs`.
- Have the projector implement the registration lifecycle domain-event handler interfaces and append projection rows directly in the current unit of work.
- Keep activity-log projection synchronous and transactionally consistent with the originating aggregate change.
- Do not introduce Inbox processing for this projection because these are in-process domain events, not redeliverable integration events.
- Update architecture conventions/tests as needed so multi-event domain projectors can use `*Projector` naming.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `activity-log`: Clarify that the activity log is implemented as an application projection/read model rather than a domain entity or command-side use case, and store it in the `activity_log_view` table. API behavior remains unchanged.

## Impact

- Affected code: Registrations activity-log entity/configuration, `IRegistrationsWriteStore`, registration-detail query, activity-log projection tests, architecture naming tests.
- Affected docs: `docs/arc42/08-crosscutting-concepts.md` should document projection placement and allow `*Projector` for multi-event domain-event projectors.
- Affected specs: `openspec/specs/activity-log/spec.md` should be clarified to align implementation terminology with the existing read-side projection requirement.
- Affected database: EF migration renames `registrations.activity_log` to `registrations.activity_log_view` while preserving columns, indexes, and data. Production compatibility/backfill constraints are not required because the system is not in production yet.
- No API contract or user-visible behavior change is intended.
