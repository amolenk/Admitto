using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

public sealed record TicketedEventDetailsChangedDomainEvent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    uint TicketedEventVersion,
    EventName Name,
    AbsoluteUrl WebsiteUrl,
    Slug PublicSlug,
    TimeZoneId TimeZone) : DomainEvent;
