## Context

The `activity-log` capability is already specified as a read-side projection in the Registrations module. The current code stores the row shape as `Registrations/Domain/Entities/ActivityLog` and appends entries through a `WriteActivityLog` command/handler slice triggered by domain-event handlers.

This is functionally correct but semantically misleading: activity-log rows are derived timeline data, not an aggregate or domain entity that enforces business invariants. The projection is still intentionally synchronous because the registration-detail query should see lifecycle entries committed atomically with the originating registration mutation.

## Goals / Non-Goals

**Goals:**

- Make the activity-log implementation reflect its read-side projection role.
- Remove the `ActivityLog` domain entity and `WriteActivityLog` command slice.
- Rename the backing table to `activity_log_view` while keeping the query response behavior unchanged.
- Keep projection writes in the same database transaction as the originating domain event.
- Allow a multi-event domain projector class to use `*Projector` naming.

**Non-Goals:**

- No API response contract change.
- No asynchronous/eventual-consistency redesign.
- No Inbox use for in-process domain events.
- No new general read-model infrastructure beyond this projection convention.

## Decisions

### Use an application projection view type

Create `Registrations/Application/Projections/ActivityLog/ActivityLogView.cs` for the EF entity mapped to `activity_log_view`. It should contain only persisted projection state and construction needed by the projector. It is not an aggregate and should not raise domain events or enforce domain behavior.

Alternative considered: keep `ActivityLog` in `Domain/Entities` and rename the command slice. This preserves existing structure but keeps the core confusion: derived read-model state appears in the domain model.

### Rename the table to `activity_log_view`

Map the projection to `registrations.activity_log_view` instead of the existing `registrations.activity_log` table. The class name and table name should both signal that this is a read model/projection. Because the product is not in production yet, the migration can be straightforward and does not need compatibility views, dual writes, or backfill logic beyond preserving existing local/test data where possible.

Alternative considered: keep `activity_log` to avoid migration churn. That keeps the database name ambiguous and misses the current opportunity to make projection storage explicit while changes are still cheap.

### Use a single synchronous domain-event projector

Create `Registrations/Application/Projections/ActivityLog/ActivityLogProjector.cs` implementing the relevant domain-event handler interfaces:

- `IDomainEventHandler<AttendeeRegisteredDomainEvent>`
- `IDomainEventHandler<RegistrationReconfirmedDomainEvent>`
- `IDomainEventHandler<RegistrationCancelledDomainEvent>`
- `IDomainEventHandler<TicketsChangedDomainEvent>`

Each handler method appends one `ActivityLogView` row to the Registrations write store. The projector runs through the existing `DomainEventsInterceptor`, so the projection row is committed in the same unit of work as the aggregate change.

Alternative considered: keep per-event handler classes that delegate to a helper. This fits the current naming tests, but recreates the indirection we want to remove and spreads one projection across several files.

### Do not use Inbox for domain-event projection

The existing Inbox (`IInbox`) is keyed by `IIntegrationEvent.IntegrationEventId` and is meant for redeliverable queue messages. Domain events are in-process and dispatched inside the save transaction; they are not retried independently from the transaction. Adding Inbox semantics here would require inventing synthetic message keys for non-message events and would conflate two different reliability models.

If the save transaction rolls back, both the aggregate change and projection row roll back. If it commits, both are committed. That is the required consistency model for this projection.

Alternative considered: convert activity-log writes to integration-event consumers and use Inbox. This would make the log eventually consistent and could leave registration detail briefly without its activity entry, which conflicts with the current synchronous projection requirement.

### Extend architecture naming rules for projectors

The current architecture tests allow multi-event domain handlers named `*Publisher`, but not `*Projector`. Update the naming test and `docs/arc42/08-crosscutting-concepts.md` to allow multi-event `IDomainEventHandler<T>` implementors named `*Projector` when their purpose is maintaining an application projection/read model.

Alternative considered: name the class `ActivityLogPublisher` to satisfy the current rule. That is inaccurate because the class writes a projection; it does not publish events.

## Risks / Trade-offs

- [Risk] Moving the EF type namespace and renaming the table can cause EF to scaffold drop/create instead of rename operations. → Mitigation: use the EF migration tooling, inspect the generated migration, and prefer table/index rename operations so existing development data is preserved where possible.
- [Risk] A single projector with multiple handler interfaces could be missed by current handler scanning or architecture rules. → Mitigation: verify `AddDomainEventHandlersFromAssembly` registers all closed interfaces and run architecture tests first.
- [Risk] Tests that reference the old domain entity path will fail. → Mitigation: update fixtures/tests to seed/query `ActivityLogView` through the write store or DbContext.
- [Risk] Projection terminology may be confused with asynchronous read-model projection. → Mitigation: document that this projection is synchronous and transactionally consistent because it is fed by domain events inside the same unit of work.

## Migration Plan

1. Rename the EF entity to `ActivityLogView` and map it to `activity_log_view`.
2. Generate an EF Core migration for the Registrations context using the approved EF migration workflow.
3. Ensure the migration renames `activity_log` to `activity_log_view` and updates the index name if needed, without changing the logical schema.
4. Rollback is the inverse table/index rename.
