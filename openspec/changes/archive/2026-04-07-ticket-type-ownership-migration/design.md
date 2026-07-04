## Context

TicketType is a value object on the `TicketedEvent` aggregate in the Organization module, but its primary consumers live in Registrations: capacity tracking, registration validation, coupon validation. The current architecture creates two coupling paths between the modules:

1. **Async capacity sync** — Organization publishes `TicketTypeAddedModuleEvent` and `TicketTypeCapacityChangedModuleEvent`, which Registrations consumes to maintain a mirrored `EventCapacity` aggregate.
2. **Synchronous facade calls** — Registration handlers call `IOrganizationFacade.GetTicketTypesAsync()` and `IsEventActiveAsync()` at request time.

This design covers moving TicketType ownership to Registrations, eliminating both coupling paths for ticket type data while introducing lightweight lifecycle events for event cancellation and archival.

### Current state

- **Organization module**: `TicketedEvent` aggregate owns `List<TicketType>` (value objects). Methods: `AddTicketType`, `UpdateTicketType`, `CancelTicketType`. Cancel cascades `IsCancelled` to all ticket types. EF stores ticket types as a JSON column. Admin API endpoints and CLI commands target Organization.
- **Registrations module**: `EventCapacity` aggregate mirrors ticket type capacity via module events. `TicketCapacity` child entities track `MaxCapacity` and `UsedCapacity`. Registration handlers call Organization facade for ticket type data and event status.
- **Cross-module contracts**: `TicketTypeDto`, `TicketTypeAddedModuleEvent`, `TicketTypeCapacityChangedModuleEvent` in Organization.Contracts. `IOrganizationFacade` exposes `GetTicketTypesAsync` and `IsEventActiveAsync`.

### Constraints

- Modular monolith with schema-per-module (ADR-001).
- Greenfield deployment — no data migration needed.
- Module events use outbox pattern with eventual consistency.
- `OrganizationScope` middleware resolves `teamSlug → teamId` and `eventSlug → eventId` via Organization facade — this stays.

## Goals / Non-Goals

**Goals:**
- Make ticket type CRUD a Registrations-module concern with local data access.
- Unify ticket type definition and capacity tracking into a single aggregate.
- Eliminate cross-module ticket type data sync (module events for capacity).
- Remove synchronous facade calls for ticket types during registration.
- Preserve event lifecycle coordination via lightweight module events (cancel/archive).
- Keep the admin API URL patterns unchanged.

**Non-Goals:**
- Merging `TicketCatalog` with `EventRegistrationPolicy` into a single aggregate (rejected due to concurrency contention — `Claim()` on every registration would conflict with admin policy edits).
- Cascading event cancellation to individual ticket types (the policy status is sufficient to block registrations; ticket types stay dormant).
- Eager creation of `TicketCatalog` via a `TicketedEventCreated` module event (lazy creation on first `AddTicketType` is simpler and sufficient).
- Formal ADR — this design document is the decision record.

## Decisions

### D1: New `TicketCatalog` aggregate with `TicketType` child entities

**Decision**: Create a `TicketCatalog` aggregate in Registrations.Domain, keyed by `TicketedEventId`, that owns `TicketType` child entities (mutable, keyed by slug). This replaces both the Organization `TicketType` value object and the Registrations `EventCapacity`/`TicketCapacity` entities.

```
TicketCatalog : Aggregate<TicketedEventId>
└── TicketType[] : Entity<string>  (keyed by slug)
    ├── Name : DisplayName
    ├── TimeSlots : TimeSlot[]
    ├── MaxCapacity : int?
    ├── UsedCapacity : int
    └── IsCancelled : bool
```

**Why entity over value object**: `TicketType` now tracks mutable state (`UsedCapacity` incremented on every registration, `MaxCapacity` updatable, `IsCancelled` togglable). An immutable value object would require replace-on-update semantics which is awkward for capacity mutation during `Claim()`.

**Why merge with capacity**: Ticket definition (name, time slots) and capacity (max, used) are inseparable at the domain level — you can't claim capacity without knowing the ticket type exists and isn't cancelled. Keeping them in one aggregate ensures atomic consistency.

**Alternatives considered**:
- Keep separate aggregates for ticket definition and capacity: rejected because it reintroduces the sync problem locally.
- Merge with `EventRegistrationPolicy`: rejected due to concurrency — `Claim()` on every registration bumps the version, conflicting with admin policy edits.

### D2: Event lifecycle status lives on `EventRegistrationPolicy`

**Decision**: Add an `EventLifecycleStatus` (Active/Cancelled/Archived) field to `EventRegistrationPolicy`. Lifecycle event handlers from Organization update this field. Registration handlers check it before proceeding.

**Why on the policy, not the catalog**: `EventRegistrationPolicy` already answers "can you register for this event?" — it governs registration windows, email domain restrictions, and now lifecycle status. This keeps the "gatekeeping" logic in one place.

**Consequence**: Event cancellation does NOT cascade to individual ticket types. Ticket types remain as-is (dormant). The policy status blocks all registrations regardless of individual ticket type state.

**Consequence**: `AddTicketType` and other ticket type CRUD operations check `EventRegistrationPolicy` status before allowing changes, preventing configuration of ticket types on cancelled/archived events.

### D3: Organization publishes lifecycle events only

**Decision**: Add two new module events:
- `TicketedEventCancelledModuleEvent { TicketedEventId: Guid }`
- `TicketedEventArchivedModuleEvent { TicketedEventId: Guid }`

Remove `TicketTypeAddedModuleEvent` and `TicketTypeCapacityChangedModuleEvent`.

**Event handlers**: One thin event handler per lifecycle event in Registrations, following the event→command→handler pattern. Each maps to a command that updates `EventRegistrationPolicy.EventLifecycleStatus`.

**Why not cancel individual ticket types**: The policy is the gatekeeper. Keeping ticket types dormant preserves their configuration if the event were hypothetically reactivated (though that's not currently supported).

### D4: Ticket type CRUD endpoints move to Registrations module

**Decision**: The admin API endpoints for ticket types (`POST /ticket-types`, `PUT /ticket-types/{slug}`, `POST /ticket-types/{slug}/cancel`) move from `OrganizationApiEndpoints` to `RegistrationsModule.MapRegistrationsAdminEndpoints()`. URL patterns stay the same.

**Why**: The endpoints now operate on `TicketCatalog` in the Registrations module's database schema. The `OrganizationScope` middleware still resolves team/event slugs to IDs via Organization facade — that routing concern is independent of which module owns ticket types.

### D5: Registration handlers query locally

**Decision**: `SelfRegisterAttendeeHandler`, `RegisterWithCouponHandler`, and `CreateCouponHandler` load ticket type data from `TicketCatalog` (local) instead of calling `IOrganizationFacade.GetTicketTypesAsync()`. The `IsEventActiveAsync()` check is replaced by loading `EventRegistrationPolicy` and checking `EventLifecycleStatus`.

**Removes from `IOrganizationFacade`**: `GetTicketTypesAsync`, `IsEventActiveAsync`. Facade retains: `GetTeamIdAsync`, `GetTicketedEventIdAsync`, `GetTeamMembershipRoleAsync`.

### D6: `Capacity` and `TimeSlot` value objects move to Registrations.Domain

**Decision**: These value objects move from Organization.Domain to Registrations.Domain. Organization no longer needs them after ticket types are removed. `Slug` and `DisplayName` stay in Shared.Kernel (already there, used by multiple modules).

### D7: `TicketCatalog` created lazily

**Decision**: `TicketCatalog` is created on the first `AddTicketType` call. If no ticket types exist, the aggregate doesn't exist in the database. Registration handlers handle a missing catalog gracefully (no ticket types available = cannot register).

### D8: TicketedEvent.Cancel() loses cascade logic

**Decision**: The `TicketedEvent.Cancel()` method in Organization no longer cascades `IsCancelled` to ticket types (it has no ticket types). It sets `Status = Cancelled` and raises `TicketedEventCancelledDomainEvent`. The domain event is mapped to `TicketedEventCancelledModuleEvent` via `OrganizationMessagePolicy`. Same pattern for `Archive()`.

## Risks / Trade-offs

**[Eventual consistency on lifecycle events]** → When Organization cancels an event, there's a brief window (typically sub-second in a monolith) where the Registrations module hasn't processed the cancellation event yet. A registration could slip through during this window. **Mitigation**: Accept and compensate. A future change will implement cancellation cascade — when an event is cancelled, all existing registrations are cancelled and attendees notified. Any registration that slips through the race window is indistinguishable from one made seconds before cancellation; both are handled by the same cascade mechanism. No additional complexity is needed in this design.

**[TicketCatalog wider than EventCapacity]** → The new aggregate stores more data (name, time slots) than the old capacity-only aggregate. JSON column grows. **Mitigation**: Marginal for the expected data volumes. No performance concern for small events.

**[Admin managing ticket types checks EventRegistrationPolicy]** → `AddTicketType` loads `EventRegistrationPolicy` from a different aggregate/table to check lifecycle status. This is a read across aggregates within the same module. **Mitigation**: Same DbContext, same transaction. The read is cheap and consistent within the module boundary.

**[TicketType as entity requires EF configuration changes]** → The `EventCapacity` table currently stores `TicketCapacity` as JSON with (slug, max_capacity, used_capacity). The new `TicketCatalog` stores richer JSON with (slug, name, time_slots, max_capacity, used_capacity, is_cancelled). **Mitigation**: Greenfield deployment — just update the EF configuration and migration. No data migration needed.

**[CLI commands need updating]** → CLI ticket type commands currently call Organization API endpoints. They need to target the same URLs but through the Registrations module. **Mitigation**: URL patterns are unchanged, so the CLI HTTP client doesn't need path changes — just the server-side routing changes.
