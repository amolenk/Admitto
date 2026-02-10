using Amolenk.Admitto.Registrations.Domain.Entities;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Registrations.Application.Tests.Builders;

public class TicketedEventCapacityBuilder
{
    private TicketedEventId _eventId = new(Guid.NewGuid());
    private readonly Dictionary<Guid, int> _ticketTypeCapacities = [];

    public TicketedEventCapacityBuilder WithEventId(TicketedEventId eventId)
    {
        _eventId = eventId;
        return this;
    }

    public TicketedEventCapacityBuilder WithTicketTypeCapacity(Guid ticketTypeId, int capacity)
    {
        _ticketTypeCapacities[ticketTypeId] = capacity;
        return this;
    }

    public EventCapacity Build()
    {
        var capacity = EventCapacity.Create(_eventId);
        foreach (var (ticketTypeId, maxCapacity) in _ticketTypeCapacities)
        {
            capacity.SetTicketTypeCapacity(new TicketTypeId(ticketTypeId), maxCapacity);
        }

        return capacity;
    }
}