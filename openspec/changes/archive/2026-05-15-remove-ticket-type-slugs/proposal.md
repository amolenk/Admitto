## Why

TicketType still uses a user-supplied slug as its primary key — a leftover from the CLI era when human-readable identifiers were convenient to type. Now that the Admin UI handles all interactions, slugs add no value and introduce unnecessary friction (format constraints, immutability, uniqueness wording in errors). Email templates went through this same cleanup already.

## What Changes

- **BREAKING** `TicketType` identity changes from a user-supplied slug string to a server-generated `TicketTypeId` (GUID). The `Slug` field is removed from the add-ticket-type request; the server assigns an ID on creation.
- **BREAKING** `TicketType.Name` becomes unique within its `TicketCatalog` (case-insensitive), replacing the uniqueness guarantee previously carried by the slug.
- The `TimeSlot` value object is replaced: the current `sealed record TimeSlot(Slug)` wrapper becomes a proper Vogen `[ValueObject<string>]` struct with its own validation (non-empty, max length), removing the slug format constraint on time slot identifiers.
- `TicketType.TimeSlotSlugs: Slug[]` + computed `TimeSlots` property collapse into a single `TimeSlots: TimeSlot[]` stored directly.
- `TicketTypeSnapshot` fields update from `(Slug, TicketTypeName, Slug[])` to `(TicketTypeId, TicketTypeName, TimeSlot[])`.
- `Coupon.AllowedTicketTypeSlugs` renamed to `AllowedTicketTypeIds` and typed as `List<TicketTypeId>`.
- All API endpoints, commands, and query filters that previously accepted slug strings now accept GUIDs.
- Integration event `TicketTypeItem` payload changes from `(slug, name)` to `(id, name)`.
- BulkEmail recipient filter changes from `TicketTypeSlugs` to `TicketTypeIds`.

## Capabilities

### New Capabilities

<!-- none -->

### Modified Capabilities

- `ticket-type-management`: Slug removed as identity; Name becomes the unique identifier within a catalog; `TimeSlot` VO format constraints lifted.
- `coupon-management`: `AllowedTicketTypeSlugs` replaced by `AllowedTicketTypeIds` in coupon creation and response contracts.
- `change-attendee-tickets`: `TicketTypeSlugs` in request replaced by `TicketTypeIds`.
- `registration-listing`: `TicketTypeSlugs` filter parameter replaced by `TicketTypeIds`; `RegistrationListItemDto` updated accordingly.

## Impact

- **Domain**: `TicketType`, `TicketCatalog`, `TicketTypeSnapshot`, `Coupon`, `TimeSlot` VO, domain events
- **Application**: All ticket type management handlers/commands/validators, `RegisterAttendeeHandler`, `ChangeAttendeeTicketsHandler`, `CreateCouponHandler`, `GetRegistrationsHandler`, `RegistrationsFacade`, `RegistrationsIntegrationEventPublisher`
- **Contracts**: `RegistrationListItemDto`, `QueryRegistrationsDto`, integration event contracts
- **Email module**: `BulkEmailSourceHttpDto`, `BulkEmailRecipientResolver`
- **API**: Ticket type routes change from slug-parameterised paths to `{id:guid}` paths
- **Database**: EF migration to change `ticket_types` PK from `varchar` slug to `uuid`, add unique index on `(ticketed_event_id, lower(name))`, update `registrations` ticket snapshot columns, update `coupons` allowed ticket type ids column
- **Greenfield** — no live data to migrate
