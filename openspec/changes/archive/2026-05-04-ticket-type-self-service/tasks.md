## 1. Domain — TicketType Entity

- [x] 1.1 Add `SelfServiceEnabled: bool` property to `TicketType` and update the internal constructor to accept and set it (default `true`)
- [x] 1.2 Add `UpdateSelfServiceEnabled(bool enabled)` method to `TicketType`
- [x] 1.3 Remove the `MaxCapacity is null` guard from `ClaimWithEnforcement()` (capacity check only: `UsedCapacity >= MaxCapacity` when not null)
- [x] 1.4 Add self-service enforcement to `TicketCatalog.Claim()`: when `enforce: true`, collect all slugs where `!SelfServiceEnabled` and throw `BusinessRuleViolationException(Errors.TicketTypesNotSelfService(slugs))` before the per-ticket loop
- [x] 1.5 Add `TicketCatalog.Errors.TicketTypesNotSelfService(string[] slugs)` error entry (error code `ticket_type.not_self_service`)
- [x] 1.6 Update `TicketCatalog.UpdateTicketType()` to accept and apply `bool selfServiceEnabled`

## 2. EF Configuration

- [x] 2.1 Add `SelfServiceEnabled` JSON property mapping in `TicketCatalogEntityConfiguration` (`HasJsonPropertyName("self_service_enabled")`)
- [x] 2.2 Generate an EF Core migration to update the model snapshot — no SQL schema change required since ticket types live in a JSON column; the migration will be empty or near-empty but keeps the snapshot in sync

## 3. Application — AddTicketType

- [x] 3.1 Add `bool SelfServiceEnabled` to `AddTicketTypeCommand`
- [x] 3.2 Update `AddTicketTypeHandler` to pass `SelfServiceEnabled` to the domain call
- [x] 3.3 Add `SelfServiceEnabled` field to `AddTicketTypeHttpRequest` (required, bool)
- [x] 3.4 Update `AddTicketTypeValidator` to validate `SelfServiceEnabled` (must be present)

## 4. Application — UpdateTicketType

- [x] 4.1 Add `bool? SelfServiceEnabled` to `UpdateTicketTypeCommand` (nullable — only update when provided)
- [x] 4.2 Update `UpdateTicketTypeHandler` to call `UpdateSelfServiceEnabled` when the value is provided
- [x] 4.3 Add `SelfServiceEnabled` field to the `UpdateTicketTypeHttpRequest`
- [x] 4.4 Update `UpdateTicketTypeValidator` if needed

## 5. Application — Admin GetTicketTypes

- [x] 5.1 Add `bool SelfServiceEnabled` to `TicketTypeDto`
- [x] 5.2 Update `GetTicketTypesHandler` to project `SelfServiceEnabled` from the domain entity

## 6. Application — Public GetTicketTypes Endpoint

- [x] 6.1 Create `PublicTicketTypeDto` record: `Slug`, `Name`, `TimeSlots`, `MaxCapacity`, `UsedCapacity`
- [x] 6.2 Create `GetPublicTicketTypesQuery(TicketedEventId EventId)`
- [x] 6.3 Create `GetPublicTicketTypesHandler` — returns active (`!IsCancelled`), self-service-enabled (`SelfServiceEnabled`) ticket types only; returns 404 if catalog not found
- [x] 6.4 Create `GetPublicTicketTypesHttpEndpoint` mapping `GET` on the event group; resolve team+event slugs to IDs using the existing scope/helper pattern; return `Ok<IReadOnlyList<PublicTicketTypeDto>>`
- [x] 6.5 Wire up `.MapGetPublicTicketTypes()` in `RegistrationsModule.MapRegistrationsPublicEndpoints()`

## 7. Tests

- [x] 7.1 Update existing domain/unit tests for `TicketType` and `TicketCatalog` to pass `selfServiceEnabled: true` in constructor calls
- [x] 7.2 Add domain test: `Claim(enforce: true)` with a non-self-service ticket type throws `TicketTypesNotSelfService`
- [x] 7.3 Add domain test: `Claim(enforce: false)` with a non-self-service ticket type succeeds (admin/coupon bypass)
- [x] 7.4 Add API test (SC): public endpoint returns only active + self-service-enabled ticket types (covers proposal SC scenarios)
- [x] 7.5 Add API test (SC): self-service registration rejected with `ticket_type.not_self_service` when ticket type has `SelfServiceEnabled = false`

## 8. Admin UI

- [x] 8.1 Regenerate the Admin UI SDK: `aspire start --isolated` → `aspire wait api` → `curl spec` → `pnpm openapi-ts`
- [x] 8.2 Update `addSchema` in `ticket-types-section.tsx`: add `selfServiceEnabled: z.boolean().default(true)` and replace `maxCapacity: z.coerce.number().int().positive().optional()` with `limitCapacity: z.boolean().default(false)` + `maxCapacity: z.number().int().min(1).optional()`
- [x] 8.3 Update `editSchema` the same way
- [x] 8.4 Update `AddTicketTypeForm`: add "Enable self-service registration" checkbox (default checked); add "Limit capacity" checkbox that reveals a numeric input when checked; wire `maxCapacity` to `null` when `limitCapacity` is unchecked
- [x] 8.5 Update `EditTicketTypeForm`: same toggles, pre-fill from `ticketType.selfServiceEnabled` and `ticketType.maxCapacity != null`
- [x] 8.6 Update the ticket type list row to show a self-service badge/icon (e.g., a globe icon or "Self-service" pill when enabled, muted "Admin only" text when disabled)
- [x] 8.7 Verify the Admin UI builds with no TypeScript errors (`pnpm build`)
