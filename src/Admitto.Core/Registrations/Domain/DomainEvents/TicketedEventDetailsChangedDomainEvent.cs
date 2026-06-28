using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

public sealed record TicketedEventDetailsChangedDomainEvent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    uint TicketedEventVersion,
    EventName Name,
    AbsoluteUrl WebsiteUrl,
    Slug PublicSlug,
    TimeZoneId TimeZone) : DomainEvent
{
    public TicketedEventDetailsChangedDomainEvent(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        EventName name,
        AbsoluteUrl websiteUrl,
        Slug publicSlug,
        TimeZoneId timeZone)
        : this(teamId, ticketedEventId, TicketedEventVersion: 0, name, websiteUrl, publicSlug, timeZone)
    {
    }
}
