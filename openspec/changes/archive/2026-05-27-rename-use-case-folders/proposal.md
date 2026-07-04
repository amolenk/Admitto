## Why

Use case folder names across modules follow at least four different naming patterns (`[Entity]Management`, plural nouns, singular nouns, verb phrases), making the codebase inconsistent and harder to navigate. Establishing a single convention now, while the codebase is still growing, prevents the inconsistency from compounding further.

## What Changes

- Remove the `*Management` suffix from all use case group folders — replace with the plural noun of the entity (e.g., `TeamManagement` → `Teams`, `CouponManagement` → `Coupons`)
- Pluralize `Waitlist` → `Waitlists` for consistency with other entity-group folders
- Restructure `Email/UseCases/SendEmail` (a use case incorrectly sitting at group level) by wrapping it under a new `Emails/` parent group
- Merge `AttendeeEmails/GetAttendeeEmails` into the new `Emails/` group and delete the now-redundant `AttendeeEmails/` folder
- Update all C# namespace declarations and `using` statements throughout the codebase to match the renamed paths

**Full rename map:**

| Module | Before | After |
|--------|--------|-------|
| Organization | `ApiKeyManagement` | `ApiKeys` |
| Organization | `TeamManagement` | `Teams` |
| Organization | `TeamMembershipManagement` | `TeamMemberships` |
| Organization | `TicketedEventManagement` | `TicketedEvents` |
| Registrations | `CouponManagement` | `Coupons` |
| Registrations | `TicketedEventManagement` | `TicketedEvents` |
| Registrations | `TicketTypeManagement` | `TicketTypes` |
| Registrations | `Waitlist` | `Waitlists` |
| Email | `SendEmail/` (group level) | `Emails/SendEmail/` |
| Email | `AttendeeEmails/GetAttendeeEmails/` | `Emails/GetAttendeeEmails/` |
| Badges | `BadgeInstanceManagement` | `BadgeInstances` |
| Badges | `BadgeTypeManagement` | `BadgeTypes` |

## Capabilities

### New Capabilities
<!-- None — this is a pure refactoring with no new capabilities -->

### Modified Capabilities
<!-- No spec-level behavior changes — this is a structural/naming refactoring only -->

## Impact

- **~250 C# files** across `src/Admitto.Core/` and `tests/` will need namespace and/or `using` updates
- **No behavioral changes** — this is a pure rename/restructure; all logic, endpoints, and contracts remain identical
- **No API surface changes** — only internal namespace paths change
- **Architecture tests** are not affected (they do not reference specific folder/namespace names)
