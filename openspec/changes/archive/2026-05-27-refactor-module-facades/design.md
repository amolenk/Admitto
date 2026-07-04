## Context

The cross-module facades (`IRegistrationsFacade`, `IOrganizationFacade`) are the only
sanctioned way for modules to call into one another. Over time, individual use cases
added their own dedicated facade methods and consumer-specific DTOs, resulting in a
facade that mirrors its callers rather than exposing a stable, reusable contract.

Current problems:
- `QueryRegistrationsForBadgeExportAsync` is functionally identical to
  `QueryRegistrationsAsync` with a fixed filter and a narrower projection — it exists
  only to serve one caller.
- `BadgeExportRegistrationDto` is a projection that belongs to the Badges caller, not
  the Registrations contract boundary.
- `GetTicketedEventEmailContextAsync` embeds "Email" in a Registrations facade method
  name, leaking knowledge of its single consumer.
- Several method parameters accept domain value objects (`TicketedEventId`,
  `TicketTypeId`) — these are internal domain types that should not cross module
  contract boundaries.
- `ValidateApiKeyAsync` describes the caller's intent rather than the operation
  performed (returning an owner id).

## Goals / Non-Goals

**Goals:**
- Facade methods are named after what they do, not who calls them.
- No domain value objects on facade method signatures — use `Guid`.
- No consumer-specific DTOs on the facade contract — callers project what they need.
- Redundant methods are consolidated; the remaining methods are each reusable by
  multiple potential callers.

**Non-Goals:**
- Changing any user-visible behaviour.
- Changing what data is returned by existing methods (shapes stay the same except
  where a dedicated method is replaced by a general one).
- Generalising the `ReconfirmTriggerSpec` methods further — the domain language in
  those types is an acceptable and intentional coupling.

## Decisions

### D1 — Remove `QueryRegistrationsForBadgeExportAsync`; use `GetRegistrationsAsync`

`QueryRegistrationsForBadgeExportAsync(eventId, ticketTypeIds[])` is equivalent to
`QueryRegistrationsAsync(eventId, new QueryRegistrationsDto(RegistrationStatus: Registered, TicketTypeIds: ticketTypeIds))`.
The Badges handler already has access to the general method. The only difference was
the narrower return type (`BadgeExportRegistrationDto`) — but that projection
(FirstName, LastName, Email, AdditionalDetails) is a strict subset of
`RegistrationListItemDto`, so the caller can project locally.

**Alternative considered**: Keep both methods for "clarity of intent". Rejected because
it creates a maintenance surface (two code paths doing the same query) and sets a
pattern where every new consumer adds its own method.

### D2 — Replace `GetTicketedEventEmailContextAsync` with `GetEventRegistrationSnapshotAsync`

The combined event + registration read is justified as a performance optimisation (one
round-trip). The name change removes the consumer reference while preserving the
semantics. `TicketedEventEmailContextDto` → `EventRegistrationSnapshotDto`.

### D3 — Facade method parameters use `Guid`, not domain VOs

Even shared domain value objects (`TicketedEventId`) are internal to the domain layer.
Placing them on a cross-module contract creates an implicit dependency on the domain
model of the providing module. All facade method signatures use `Guid`; implementations
wrap to the appropriate VO internally.

**Alternative considered**: Keep typed VOs for compile-time safety. Rejected because
the benefit (type safety at the call site) is outweighed by the boundary violation, and
`Guid` is already the canonical cross-boundary primitive used everywhere else (events,
DTOs, etc.).

### D4 — Rename `ValidateApiKeyAsync` → `LookupApiKeyOwnerAsync`

The current name implies a boolean validation; the actual return is the owner `Guid?`.
The rename makes the return type self-evident.

## Risks / Trade-offs

- **Compile-time breakage at all call sites** → Mitigation: all affected call sites
  are within this repository; the compiler surfaces every broken reference. No
  runtime risk.
- **Badges handler does a local projection** → The projection is four fields from a
  richer DTO; no performance impact. The trade-off is a few lines of mapping code
  in the caller, which is appropriate — the caller decides its own view of the data.

## Migration Plan

1. Update `IRegistrationsFacade` interface and `RegistrationsFacade` implementation.
2. Remove `BadgeExportRegistrationDto`; rename `TicketedEventEmailContextDto`.
3. Update `IOrganizationFacade`, `OrganizationFacade`, and `CachingOrganizationFacade`.
4. Update all callers — compiler errors guide each site:
   - `ExportBadgeCsvHandler` (Badges)
   - `ApiKeyAuthenticationHandler` (Api)
   - Email module event handlers and jobs that call the renamed snapshot method
   - All sites passing `TicketedEventId` — pass `.Value` instead
5. Run architecture tests and all affected module test suites.

No deployment or data migration steps required — purely a code-level refactor.
