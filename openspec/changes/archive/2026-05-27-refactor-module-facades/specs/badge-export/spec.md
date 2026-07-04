## REMOVED Requirements

### Requirement: IRegistrationsFacade exposes a badge-export query
**Reason**: `QueryRegistrationsForBadgeExportAsync` is redundant with the general
`GetRegistrationsAsync` method. `BadgeExportRegistrationDto` was a caller-specific
projection that does not belong on the cross-module contract boundary. The Badges
handler now calls `GetRegistrationsAsync` with an equivalent filter and projects
`RegistrationListItemDto` locally.

**Migration**: Replace calls to `QueryRegistrationsForBadgeExportAsync(eventId, ticketTypeIds)`
with `GetRegistrationsAsync(eventId.Value, new QueryRegistrationsDto(RegistrationStatus: Registered, TicketTypeIds: ticketTypeIds.Select(id => id.Value).ToList()))` and project the needed fields (`FirstName`, `LastName`, `Email`, `AdditionalDetails`) from `RegistrationListItemDto` at the call site.
