## Context

Ticket types live on the `TicketCatalog` aggregate in `Admitto.Module.Registrations`. The current self-service gate is implicit: `TicketType.ClaimWithEnforcement()` throws `TicketTypeNotAvailable` when `MaxCapacity is null`. This conflates two separate concerns — capacity limits and self-service availability — making unlimited self-service impossible and giving external websites no way to discover available ticket types without attempting a registration.

Two code paths call into capacity claiming:
- `catalog.Claim(slugs, enforce: true)` — self-service (registration + change tickets)
- `catalog.Claim(slugs, enforce: false)` — admin and coupon (calls `ClaimUncapped`, bypasses all enforcement)

The Admin UI currently renders a plain optional number input for capacity, which cannot reliably be cleared once set.

## Goals / Non-Goals

**Goals:**
- Decouple self-service availability from capacity limits with an explicit `SelfServiceEnabled` flag
- Expose a public endpoint for external websites to list self-service-available ticket types
- Enforce the flag in all `enforce: true` claim paths
- Fix the capacity toggle UX in the Admin UI

**Non-Goals:**
- Changing coupon or admin registration flows (they continue to bypass via `ClaimUncapped`)
- Price fields, time-slot management UI

## Decisions

### D1: Flag lives on `TicketType`, not `TicketCatalog`
`SelfServiceEnabled` is per-ticket-type intent, not per-event policy. Placing it on `TicketType` alongside `IsCancelled` and `MaxCapacity` is consistent and keeps all ticket-type attributes co-located.

_Alternative considered_: A separate policy or a per-event "self-service mode" flag — rejected because the requirement is per-ticket-type granularity.

### D2: Self-service check at `TicketCatalog.Claim()` validation stage, not inside `ClaimWithEnforcement()`
The validation stage in `Claim()` already collects all cancelled slugs in one pass and throws a batch error. Adding the self-service check at the same stage (when `enforce: true`) keeps the pattern consistent and reports all non-self-service slugs at once rather than failing on the first one.

```
// New validation step in Claim(), after cancelled check:
if (enforce)
{
    var nonSelfService = slugs.Where(s => !ticketTypeMap[s].SelfServiceEnabled).ToArray();
    if (nonSelfService.Length > 0)
        throw new BusinessRuleViolationException(Errors.TicketTypesNotSelfService(nonSelfService));
}
```

`ClaimWithEnforcement()` retains only the capacity check; the `MaxCapacity is null` guard is removed.

_Alternative considered_: Keep the check inside `ClaimWithEnforcement()` — rejected because it fails one-by-one and the error message would only name the first offending slug.

### D3: EF migration default = `true`
Greenfield deployment. All existing ticket types (in dev/staging) default to `SelfServiceEnabled = true` for zero-friction migration. The EF column gets `defaultValue: true`.

_Alternative considered_: Default to `MaxCapacity != null` to exactly preserve old implicit behavior — not needed for greenfield; adds migration complexity with no benefit.

### D4: Public endpoint is API-key authenticated (existing pattern)
The public endpoint group already requires API key auth via `ApiKeyTeamScopeFilter`. The new `GET /events/{teamSlug}/{eventSlug}/ticket-types` endpoint slots into `MapRegistrationsPublicEndpoints()` following the same pattern. External websites already have API keys for the other self-service endpoints.

### D5: New `PublicTicketTypeDto` (separate from admin `TicketTypeDto`)
The public response omits admin-only fields (`isCancelled`, `usedCapacity` breakdown) and is shaped for external consumption. A dedicated DTO avoids leaking admin fields and allows the public contract to evolve independently.

```csharp
record PublicTicketTypeDto(
    string Slug,
    string Name,
    string[] TimeSlots,
    int? MaxCapacity,
    int UsedCapacity);
```

### D6: UI — "Limit capacity" checkbox + "Enable self-service" checkbox (inline)
Both toggles are added inline to the existing add/edit forms on the Registration tab. No new page or modal. The capacity toggle replaces the plain optional number input, fixing the clear-capacity bug. If the design feels cramped, a follow-up can move to a per-ticket-type detail panel.

## Risks / Trade-offs

- **Existing registrations referencing now-disabled ticket types**: Releasing capacity on cancel always works; the flag is only checked at claim time. No risk to existing data.
- **External websites may cache the public ticket-types response**: No cache-control headers today; not in scope, but worth noting for future.
- **UI inline density**: Two new checkboxes + existing slug/name/capacity fields may feel crowded on small screens. Mitigation: use a 2-row layout (slug+name on row 1, toggles+capacity on row 2).

## Migration Plan

1. Add EF Core migration: `ALTER TABLE TicketTypes ADD SelfServiceEnabled BIT NOT NULL DEFAULT 1`
2. Update domain, application, and API layers (no data fix-up needed — default covers all rows)
3. Deploy backend
4. Regenerate Admin UI SDK (`pnpm openapi-ts`)
5. Deploy Admin UI

No rollback complexity: the column has a safe default and no existing code reads it until the new validation path is live.

## Open Questions

_None — all decisions resolved during exploration._
