## Context

The codebase adopted Vogen for strong-typed value objects. Most IDs and semantic primitives already have dedicated VOs, but a number of entities and domain events still carry raw `string`, `int`, or `Guid` fields where a typed VO either already exists or should be introduced. Additionally, the shared `DisplayName` VO is reused for three distinct domain concepts (team name, event name, ticket type name), erasing meaningful type boundaries. This design covers the mechanical steps needed to complete the migration.

Current primitives targeted for replacement:

| Location | Field | Current type | Target type |
|---|---|---|---|
| `TicketedEventCreatedDomainEvent` | `CreationRequestId` | `Guid` | `CreationRequestId` (exists) |
| `OtpCodeRequestedDomainEvent` | `EventName` | `string` | `EventName` (new, from split) |
| `ActivityLog` | `RegistrationId` | `Guid` | `RegistrationId` (exists) |
| `TicketType` entity key | — | `Entity<string>` | `Entity<TicketTypeId>` (exists) |
| `TicketType` | `TimeSlotSlugs` | `string[]` | `Slug[]` (exists) |
| `TicketTypeSnapshot` | `Slug`, `Name`, `TimeSlots` | `string`, `string`, `string[]` | `Slug`, `TicketTypeName`, `Slug[]` |
| `ExternalListItem` | `Email` | `string` | `EmailAddress` (exists) |
| `BulkEmailRecipient` | `Email`, `RegistrationId?` | `string`, `Guid?` | `EmailAddress`, `RegistrationId?` |
| `EmailLog` | `TeamId`, `TicketedEventId`, `RegistrationId?`, `Recipient` | `Guid`, `Guid`, `Guid?`, `string` | `TeamId`, `TicketedEventId`, `RegistrationId?`, `EmailAddress` |
| `EmailSettings` | `ScopeId` | `Guid` | `EmailScopeId` (new) |
| `EmailTemplate` | `ScopeId` | `Guid` | `EmailScopeId` (new) |
| `ApiKey` | `Name` | `string` | `ApiKeyName` (new) |
| `Team` | `Name` | `DisplayName` | `TeamName` (new, from split) |
| `TicketedEvent` | `Name` | `DisplayName` | `EventName` (new, from split) |
| `TicketType` | `Name` | `DisplayName` | `TicketTypeName` (new, from split) |
| `TicketedEventCreationRequestedDomainEvent` | `DisplayName` | `DisplayName` | `EventName` |

## Goals / Non-Goals

**Goals:**
- Every entity ID field and every semantic primitive in an entity or domain event uses a typed Vogen VO
- `DisplayName` is removed and replaced by three domain-specific name VOs
- All composite VOs (`TicketTypeSnapshot`, `ExternalListItem`, `BulkEmailRecipient`) use typed VOs for their semantic fields
- No behavioral changes; only structural type improvements

**Non-Goals:**
- Converting cryptographic material: `ApiKey.KeyHash`, `ApiKey.KeyPrefix`, `OtpCode.EmailHash`, `OtpCode.CodeHash`, `TicketedEvent.SigningKey` remain `string` — these are opaque technical values with no domain validation
- Converting free-text content: email subject/body, template content, rejection reasons remain `string`
- Changing DB column types — the underlying primitives stay the same; only EF Core value converters and application code change
- Converting `ApiKey.CreatedBy` — this field stores an external identity string (e.g. username from JWT) and does not map cleanly to any existing VO; deferred
- Changing existing serialised JSON representation of domain/integration events in the outbox

## Decisions

### 1. DisplayName split location

`TeamName` and `EventName` are cross-module concepts (both appear in contracts and the shared domain event `TicketedEventCreationRequestedDomainEvent`). They are placed in `Shared/Kernel/ValueObjects/`. `TicketTypeName` is Registrations-specific and lives in `Registrations/Domain/ValueObjects/`.

After the split, the existing `DisplayName` VO in `Shared/Kernel/ValueObjects/` is deleted.

### 2. EmailScopeId

`EmailSettings.ScopeId` and `EmailTemplate.ScopeId` can reference either a `TeamId` or a `TicketedEventId` depending on the `EmailSettingsScope` enum value. Using a union of the two types would require runtime switching throughout the Email module. A new `EmailScopeId` VO (Guid-backed, lives in `Email/Domain/ValueObjects/`) is introduced instead. It explicitly communicates "this is a scope identifier for email configuration" without conflating the two ownership domains.

*Alternative considered: two nullable properties (`TeamId? TeamScopeId` + `TicketedEventId? EventScopeId`). Rejected because it changes the DB column structure and adds optionality noise throughout the module.*

### 3. ApiKeyName VO

`ApiKey.Name` is a user-supplied label. A thin `ApiKeyName` VO (string-backed) is introduced in `Organization/Domain/ValueObjects/` to allow validation (non-empty, max length) to be centralised in the type.

### 4. TicketType entity key migration

`TicketTypeId` already exists in `Shared/Kernel/ValueObjects/`. `TicketType` must change from `Entity<string>` to `Entity<TicketTypeId>`. Wherever a `TicketType` is constructed, its string id must be wrapped: `TicketTypeId.From(rawString)`.

### 5. EF Core value converters

No DB migrations are required — all backing column types remain `uniqueidentifier` (Guid) or `nvarchar` (string). Value converters are automatically generated by Vogen when using the `[EfCoreConverter]` attribute, or added manually in `EntityTypeConfiguration`. Existing configurations must be audited to ensure new VO fields are mapped; a failing EF Core model validation in integration tests will surface any gaps.

### 6. Serialisation compatibility

Vogen structs serialise as their primitive backing value by default (with the appropriate JsonConverter registered). The outbox stores events as JSON. Since the targeted events already carry the Guid value, and Vogen Guid-backed VOs serialise as plain Guids, the JSON representation is unchanged. The `EventName` VO is string-backed and serialises as a plain string, matching the current `string EventName` field.

## Risks / Trade-offs

- [Wide surface area] The DisplayName split touches all three modules and all their tests. → Purely mechanical rename; no logic changes. Compiler errors guide all fix sites.
- [Missing EF converters] A newly typed VO field without a configured converter causes a runtime mapping exception. → Run ArchTests and integration tests before merging; they catch unmapped VOs.
- [TicketType key change] Code that creates `TicketTypeId.From(someString)` may fail at runtime if the stored string is not a valid Guid. → Audit all construction sites; existing data is always Guid strings so this is safe.
- [Outbox compatibility] An in-flight `OtpCodeRequestedDomainEvent` serialised before this change has `"EventName": "string value"`. After the change the field is still named `EventName` and the JSON value is still a string, so deserialisation is backward-compatible.
