using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Organization.Domain.DomainEvents;

/// <summary>
/// Raised when a team accepts a request to create a new ticketed event. Mapped to the
/// <c>TicketedEventCreationRequestedIntegrationEvent</c> integration event by
/// <c>OrganizationMessagePolicy</c> so the Registrations module can materialise the
/// aggregate.
/// </summary>
public sealed record TicketedEventCreationRequestedDomainEvent(
    CreationRequestId CreationRequestId,
    TeamId TeamId,
    EventName Name,
    AbsoluteUrl WebsiteUrl,
    AbsoluteUrl BaseUrl,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    TimeZoneId TimeZone)
    : DomainEvent;
