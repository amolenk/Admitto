using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.DomainEvents;

/// <summary>
/// Raised when a team accepts a request to create a new ticketed event. Mapped to the
/// <c>TicketedEventCreationRequested</c> integration event by
/// <c>OrganizationMessagePolicy</c> so the Registrations module can materialise the
/// aggregate.
/// </summary>
public sealed record TicketedEventCreationRequestedDomainEvent(
    CreationRequestId CreationRequestId,
    TeamId TeamId,
    Slug TeamSlug,
    Slug Slug,
    DisplayName Name,
    AbsoluteUrl WebsiteUrl,
    AbsoluteUrl BaseUrl,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    TimeZoneId TimeZone)
    : DomainEvent;
