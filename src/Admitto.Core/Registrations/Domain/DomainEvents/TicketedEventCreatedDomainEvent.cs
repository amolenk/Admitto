using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

/// <summary>
/// Raised by <see cref="Entities.TicketedEvent"/> when it is first created.
/// Carries the <paramref name="CreationRequestId"/> correlation id so that
/// <c>RegistrationsIntegrationEventPublisher</c> can include it in the outbound
/// <c>TicketedEventCreatedIntegrationEvent</c> without persisting it on the aggregate.
/// </summary>
public record TicketedEventCreatedDomainEvent(
    CreationRequestId CreationRequestId,
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    uint TicketedEventVersion,
    EventName Name,
    AbsoluteUrl WebsiteUrl,
    Slug PublicSlug,
    TimeZoneId TimeZone) : DomainEvent
{
    public TicketedEventCreatedDomainEvent(
        CreationRequestId creationRequestId,
        TeamId teamId,
        TicketedEventId ticketedEventId,
        TimeZoneId timeZone)
        : this(
            creationRequestId,
            teamId,
            ticketedEventId,
            TicketedEventVersion: 0,
            EventName.From("Unknown event"),
            AbsoluteUrl.From("https://example.com"),
            Slug.From("unknown-event"),
            timeZone)
    {
    }

    public TicketedEventCreatedDomainEvent(
        CreationRequestId creationRequestId,
        TeamId teamId,
        TicketedEventId ticketedEventId,
        EventName name,
        AbsoluteUrl websiteUrl,
        Slug publicSlug,
        TimeZoneId timeZone)
        : this(creationRequestId, teamId, ticketedEventId, TicketedEventVersion: 0, name, websiteUrl, publicSlug, timeZone)
    {
    }
}
