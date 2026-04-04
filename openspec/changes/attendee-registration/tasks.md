## 1. Organization module — Remove obsolete ticket type flags

- [x] 1.1 Remove `IsSelfService` and `IsSelfServiceAvailable` properties from the `TicketType` value object in `Admitto.Module.Organization`
- [x] 1.2 Remove `IsSelfService` from `AddTicketTypeCommand` request and `AddTicketTypeHandler`
- [x] 1.3 Remove `IsSelfServiceAvailable` from `UpdateTicketTypeCommand` request and `UpdateTicketTypeHandler`
- [x] 1.4 Remove the `TicketGrantMode` enum from `Admitto.Module.Registrations`
- [x] 1.5 Add `IReadOnlyList<string> TimeSlots` and `int? Capacity` to `TicketTypeDto` in `Admitto.Module.Organization.Contracts`
- [x] 1.6 Update `IOrganizationFacade` implementation to populate `TimeSlots` and `Capacity` from the `TicketType` value object
- [x] 1.7 Update Organization domain tests to remove all usage of `IsSelfService` / `IsSelfServiceAvailable`
- [x] 1.8 Update Organization application / API tests (e.g. add-ticket-type, update-ticket-type scenarios) to remove self-service flag fields

## 2. Organization module — Publish module events for ticket type lifecycle

- [x] 2.1 Create `TicketTypeAddedModuleEvent` record in `Admitto.Module.Organization.Contracts` (fields: `TicketedEventId`, `Slug`, `Name`, `TimeSlots`, `Capacity`)
- [x] 2.2 Create `TicketTypeCapacityChangedModuleEvent` record in `Admitto.Module.Organization.Contracts` (fields: `TicketedEventId`, `Slug`, `Capacity`)
- [x] 2.3 Publish `TicketTypeAddedModuleEvent` from `AddTicketTypeHandler` on successful ticket type addition
- [x] 2.4 Publish `TicketTypeCapacityChangedModuleEvent` from `UpdateTicketTypeHandler` only when the `Capacity` field actually changes
- [x] 2.5 Add/update Organization module tests to assert module events are published in the correct scenarios (D4 decision)

## 3. Registrations module — Registration domain entity

- [x] 3.1 Rewrite the `Registration` entity: replace the UUID-based ticket type snapshot with slug-keyed `TicketTypeSnapshot` value objects; keep `TicketedEventId`, `Email`, and `RegistrationDate`
- [x] 3.2 Rewrite `RegistrationEntityConfiguration`: fix the unique index from `email` alone to the composite `(ticketed_event_id, email)` (D6)
- [x] 3.3 Write domain-level unit tests for `Registration` creation and ticket snapshot structure

## 4. Registrations module — EventCapacity aggregate

- [x] 4.1 Rewrite `EventCapacity` aggregate: keyed by `TicketedEventId`, contains an owned collection of `TicketCapacity`, add `[Timestamp] uint Version` for optimistic concurrency (D5)
- [x] 4.2 Rewrite `TicketCapacity`: keyed by `Slug` (string), properties `int UsedCapacity` and `int? MaxCapacity`; implement `ClaimWithEnforcement()` — throws if `MaxCapacity` is null ("not available") or `UsedCapacity >= MaxCapacity` ("at capacity") (D9, D5)
- [x] 4.3 Implement `TicketCapacity.ClaimUncapped()` — always increments `UsedCapacity` regardless of `MaxCapacity` (D5)
- [x] 4.4 Implement `EventCapacity.Claim(IReadOnlyList<string> slugs, bool enforce)` — delegates to the appropriate `TicketCapacity` claim method for each slug
- [x] 4.5 Rewrite `TicketedEventCapacityEntityConfiguration`: map slug as key, nullable `max_capacity`, owned `TicketCapacity` collection as JSON or table (D3)
- [x] 4.6 Write domain-level unit tests for `EventCapacity` / `TicketCapacity` covering: enforce path (null capacity, at capacity, success), uncapped path, multi-ticket claim, optimistic concurrency version bump

## 5. Registrations module — EventRegistrationPolicy aggregate

- [x] 5.1 Create `EventRegistrationPolicy` aggregate: properties `TicketedEventId`, `RegistrationWindow?` (open/close `DateTimeOffset`), `AllowedEmailDomain?` (string); implement `SetWindow(open, close)` and `SetDomainRestriction(domain?)` domain methods (D2)
- [x] 5.2 Create `EventRegistrationPolicyEntityConfiguration` in `Admitto.Module.Registrations/Infrastructure`
- [x] 5.3 Write domain-level unit tests for `EventRegistrationPolicy` (set window, update window, set/remove domain)

## 6. Registrations module — Infrastructure wiring

- [x] 6.1 Add `DbSet<Registration>`, `DbSet<EventCapacity>`, and `DbSet<EventRegistrationPolicy>` to `RegistrationsDbContext` and expose them via `IRegistrationsWriteStore`
- [x] 6.2 Register the new `EntityTypeConfiguration` classes in `RegistrationsDbContext.OnModelCreating`
- [x] 6.3 Implement Postgres unique-constraint exception mapping for the `(ticketed_event_id, email)` constraint on `registrations` → `AlreadyExistsError` in `IPostgresExceptionMapping`

## 7. Registrations module — Capacity sync event handlers

- [x] 7.1 Create `TicketTypeAddedModuleEventHandler`: on `TicketTypeAddedModuleEvent`, load or create `EventCapacity` for the event, add a `TicketCapacity` entry with the given slug and `MaxCapacity`
- [x] 7.2 Create `TicketTypeCapacityChangedModuleEventHandler`: on `TicketTypeCapacityChangedModuleEvent`, load `EventCapacity` for the event and update the `MaxCapacity` for the matching `TicketCapacity` slug
- [x] 7.3 Register both handlers in the Registrations module message handler configuration / `RegistrationsModule`
- [x] 7.4 Write integration tests for capacity sync: capacity created on `TicketTypeAddedModuleEvent` (with and without capacity), ceiling updated on `TicketTypeCapacityChangedModuleEvent` (including null)

## 8. Registrations module — Registration policy use case

- [x] 8.1 Create `SetRegistrationPolicyCommand` (fields: `TicketedEventId`, `RegistrationWindowOpen?`, `RegistrationWindowClose?`, `AllowedEmailDomain?`) and `SetRegistrationPolicyHandler` (upsert `EventRegistrationPolicy`)
- [x] 8.2 Create `PUT /admin/events/{eventSlug}/registration-policy` endpoint in `Admitto.Module.Registrations/Application/UseCases/RegistrationPolicy/`; add FluentValidation validator (close must be after open if both supplied)
- [x] 8.3 Wire the new endpoint in the Registrations module endpoint registration (`RegistrationsModule`)
- [x] 8.4 Create `set-registration-policy` CLI command in `src/Admitto.Cli/Commands/` with options for `--event`, `--window-open`, `--window-close`, `--allowed-domain`
- [x] 8.5 Write integration tests for registration policy use case: configure window, update window, configure domain restriction, remove domain restriction, invalid window (close before open)

## 9. Registrations module — Self-service registration use case

- [x] 9.1 Create `SelfRegisterAttendeeCommand` (fields: `TicketedEventId`, `Email`, `TicketTypeSlugs`) and `SelfRegisterAttendeeHandler`: load `EventRegistrationPolicy`, enforce window and domain, load ticket types via `IOrganizationFacade`, validate selection (duplicates, unknown, cancelled, time-slot overlaps, event status), load `EventCapacity`, call `Claim(enforce: true)`, create `Registration`, save
- [x] 9.2 Create `POST /events/{eventSlug}/registrations` anonymous public endpoint; add FluentValidation validator for required fields
- [x] 9.3 Wire the new endpoint in the Registrations module
- [x] 9.4 Write integration tests covering all self-service scenarios: success, capacity full, no capacity set, before window opens, after window closes, no window configured, email domain mismatch, email domain match, multi-ticket success, duplicate ticket in selection, unknown ticket type, cancelled ticket type, overlapping time slots, cancelled event, archived event, duplicate email

## 10. Registrations module — Coupon registration use case

- [x] 10.1 Create `RegisterWithCouponCommand` (fields: `TicketedEventId`, `CouponCode`, `Email`, `TicketTypeSlugs`) and `RegisterWithCouponHandler`: load and validate coupon (existence, expiry, redemption status, revocation, allowlisted slugs), load `EventRegistrationPolicy` and conditionally enforce window (bypass flag check), load ticket types via `IOrganizationFacade`, validate selection (duplicates, unknown, cancelled, time-slot overlaps, event status), load or create `EventCapacity`, call `Claim(enforce: false)`, redeem coupon, create `Registration`, save
- [x] 10.2 Create `POST /events/{eventSlug}/registrations/coupon` anonymous public endpoint; add FluentValidation validator
- [x] 10.3 Wire the new endpoint in the Registrations module
- [x] 10.4 Write integration tests covering all coupon scenarios: success (capacity exceeded, counts toward used), expired coupon, already-redeemed coupon, revoked coupon, ticket type not allowlisted, bypasses window, respects window when flag not set, bypasses domain restriction, bypasses null capacity

## 11. Database migrations

- [x] 11.1 Add an Organization module EF migration to remove `IsSelfService` and `IsSelfServiceAvailable` from the `TicketType` owned JSON column (or mark them as ignored if stored as JSON)
- [x] 11.2 Add a Registrations module EF migration for the `registrations` table with the composite unique constraint on `(ticketed_event_id, email)` (replacing the broken single-column index from the scaffold)
- [x] 11.3 Add a Registrations module EF migration for the `event_capacity` table (and owned `ticket_type_capacities` JSON/table) keyed by slug with nullable `max_capacity`
- [x] 11.4 Add a Registrations module EF migration for the `event_registration_policy` table
- [ ] 11.5 Run `dotnet run --project src/Admitto.Migrations` against a local database and verify all migrations apply cleanly

## 12. Final verification

- [x] 12.1 Run `dotnet test tests/Admitto.Module.Organization.Domain.Tests` — all pass
- [x] 12.2 Run `dotnet test tests/Admitto.Module.Organization.Tests` — all pass
- [x] 12.3 Run `dotnet test tests/Admitto.Module.Registrations.Domain.Tests` — all pass
- [x] 12.4 Run `dotnet test tests/Admitto.Module.Registrations.Tests` — all pass
- [x] 12.5 Run `dotnet test tests/Admitto.Api.Tests` — all pass
