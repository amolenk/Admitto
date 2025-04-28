using Amolenk.Admitto.Domain.Exceptions;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents an event for which attendees can register.
/// </summary>
public class TicketedEvent : Entity
{
    private readonly List<TicketType> _ticketTypes;

    private TicketedEvent()
    {
        _ticketTypes = [];
    }
    
    private TicketedEvent(TicketedEventId id, string name, DateTimeOffset startDateTime, DateTimeOffset endDateTime,
        DateTimeOffset registrationStartDateTime, DateTimeOffset registrationEndDateTime, 
        IEnumerable<TicketType> ticketTypes)
        : base(id.Value)
    {
        Name = name;
        StartDateTime = startDateTime;
        EndDateTime = endDateTime;
        RegistrationStartDateTime = registrationStartDateTime;
        RegistrationEndDateTime = registrationEndDateTime;
        _ticketTypes = ticketTypes.ToList();
    }

    public string Name { get; private set; } = null!;
    public DateTimeOffset StartDateTime { get; private set; }
    public DateTimeOffset EndDateTime { get; private set; }
    public DateTimeOffset RegistrationStartDateTime { get; private set; }
    public DateTimeOffset RegistrationEndDateTime { get; private set; }
    public IReadOnlyCollection<TicketType> TicketTypes => _ticketTypes.AsReadOnly();

    public static TicketedEvent Create(string name, DateTimeOffset startDateTime, DateTimeOffset endDateTime,
        DateTimeOffset registrationStartDateTime, DateTimeOffset registrationEndDateTime, 
        IEnumerable<TicketType> ticketTypes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name cannot be empty.");

        if (endDateTime < startDateTime)
            throw new DomainException("End day should be greater than start day.");

        if (registrationStartDateTime >= registrationEndDateTime)
            throw new DomainException("Sales start time must be before sales end time.");

        if (registrationEndDateTime > startDateTime)
            throw new DomainException("Registration must close before the event starts.");

        var id = TicketedEventId.FromEventName(name);
        
        return new TicketedEvent(id, name, startDateTime, endDateTime, registrationStartDateTime, 
            registrationEndDateTime, ticketTypes);
    }
    
    public void AddTicketType(TicketType ticketType)
    {
        if (_ticketTypes.Any(existingTicketType => existingTicketType.Id == ticketType.Id))
        {
            throw new DomainException("Ticket type already exists.");
        }
        
        // if (ticketType.StartDateTime < StartDay.ToDateTime(TimeOnly.MinValue) ||
        //     ticketType.EndDateTime > EndDay.ToDateTime(TimeOnly.MaxValue))
        // {
        //     throw new DomainException("Ticket type session must be within the event duration.");
        // }

        _ticketTypes.Add(ticketType);
    }

    public void RemoveTicketType(Guid ticketTypeId)
    {
        var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Id == ticketTypeId);
        if (ticketType is null) return;
        
        if (ticketType.HasTicketsReserved())
        {
            throw new DomainException("Cannot remove ticket type with reserved tickets.");
        }
            
        _ticketTypes.Remove(ticketType);
    }
    
    public bool HasAvailableCapacity(TicketOrder ticketOrder)
    {
        return ticketOrder.TicketTypeIds.All(ticketTypeId => 
            _ticketTypes.Any(t => t.Id == ticketTypeId && t.HasAvailableCapacity()));
    }
    
    public bool TryReserveTickets(TicketOrder ticketOrder)
    {
        if (!HasAvailableCapacity(ticketOrder))
        {
            return false;
        }
        
        foreach (var ticketTypeId in ticketOrder.TicketTypeIds)
        {
            var ticketType = _ticketTypes.First(tt => tt.Id == ticketTypeId);
            ticketType.ReserveTicket();
        }

        return true;
    }

    public void CancelTickets(TicketOrder ticketOrder)
    {
        foreach (var ticketTypeId in ticketOrder.TicketTypeIds)
        {
            var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Id == ticketTypeId);
            ticketType?.CancelTicket();
        }
    }
}
