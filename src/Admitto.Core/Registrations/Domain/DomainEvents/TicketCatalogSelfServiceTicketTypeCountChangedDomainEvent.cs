using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

public sealed record TicketCatalogSelfServiceTicketTypeCountChangedDomainEvent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    uint TicketCatalogVersion,
    int SelfServiceTicketTypeCount) : DomainEvent;
