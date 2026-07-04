## 1. Projection Model

- [x] 1.1 Create `Registrations/Application/Projections/ActivityLog/ActivityLogView.cs` with the persisted activity-log row shape and append-only factory/constructor.
- [x] 1.2 Update `IRegistrationsWriteStore` and `RegistrationsDbContext` to expose `DbSet<ActivityLogView>` for the projection table.
- [x] 1.3 Update `ActivityLogEntityConfiguration` to map `ActivityLogView` to `activity_log_view` with the existing columns and index semantics.
- [x] 1.4 Remove the old `Registrations/Domain/Entities/ActivityLog` entity and unused `ActivityLogId` domain placement if no longer needed there.
- [x] 1.5 Generate a Registrations EF Core migration that renames `activity_log` to `activity_log_view` and updates index naming as needed without changing API-visible behavior.

## 2. Projector Implementation

- [x] 2.1 Create `Application/Projections/ActivityLog/ActivityLogProjector.cs` implementing the four activity-log domain-event handler interfaces.
- [x] 2.2 Move Registered, Reconfirmed, Cancelled, and TicketsChanged row creation logic from `WriteActivityLog` handlers into `ActivityLogProjector`.
- [x] 2.3 Delete the `WriteActivityLog` command, handler, and per-event handler classes.
- [x] 2.4 Confirm the projector does not use `IInbox` or `ProcessedMessage` for domain-event handling.

## 3. Queries And Tests

- [x] 3.1 Update `GetRegistrationDetailsHandler` and DTO mapping to query `ActivityLogView`.
- [x] 3.2 Update integration test fixtures and activity-log tests to seed/assert `ActivityLogView` instead of the old domain entity.
- [x] 3.3 Update or replace `WriteActivityLogHandlerTests` and domain-event handler tests with tests that cover `ActivityLogProjector` behavior.
- [x] 3.4 Verify no application code references `Registrations.Domain.Entities.ActivityLog`, `WriteActivityLog`, or the old `activity_log` table name except in migrations.

## 4. Architecture And Documentation

- [x] 4.1 Update architecture naming tests to allow multi-event domain-event handlers named `*Projector`.
- [x] 4.2 Update `docs/arc42/08-crosscutting-concepts.md` with the application projection/read-model placement and `*Projector` naming convention.
- [x] 4.3 Update `src/AGENTS.md` guidance if needed so event-driven projections are distinct from command slices.

## 5. Verification

- [x] 5.1 Run `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj` first and fix any architecture violations.
- [x] 5.2 Run targeted Registrations integration tests for activity-log and registration-detail behavior.
- [x] 5.3 Review the generated migration and model snapshot to confirm the intended table rename is the only database change.
- [x] 5.4 Run any additional targeted API tests that cover activity-log entries through registration flows.
