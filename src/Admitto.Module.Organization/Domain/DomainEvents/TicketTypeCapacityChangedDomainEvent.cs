using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.DomainEvents;

public record TicketTypeCapacityChangedDomainEvent(
    TicketedEventId TicketedEventId,
    string Slug,
    int? Capacity) : DomainEvent;
