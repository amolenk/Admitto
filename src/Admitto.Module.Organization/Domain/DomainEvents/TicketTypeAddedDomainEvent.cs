using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.DomainEvents;

public record TicketTypeAddedDomainEvent(
    TicketedEventId TicketedEventId,
    string Slug,
    string Name,
    string[] TimeSlots,
    int? Capacity) : DomainEvent;
