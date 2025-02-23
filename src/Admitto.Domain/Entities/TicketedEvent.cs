using System.ComponentModel.DataAnnotations;
using Amolenk.Admitto.Domain.Utilities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents an event for which attendees can register.
/// </summary>
public class TicketedEvent : AggregateRoot
{
    private readonly List<TicketType> _ticketTypes;

    private TicketedEvent(Guid id, string name, DateOnly startDay, DateOnly endDay,
        DateTime salesStartDateTime, DateTime salesEndDateTime) : base(id)
    {
        Name = name;
        StartDay = startDay;
        EndDay = endDay;
        SalesStartDateTime = salesStartDateTime;
        SalesEndDateTime = salesEndDateTime;
        _ticketTypes = [];
    }

    public string Name { get; private set; }
    public DateOnly StartDay { get; private set; }
    public DateOnly EndDay { get; private set; }
    public DateTime SalesStartDateTime { get; private set; }
    public DateTime SalesEndDateTime { get; private set; }
    public IReadOnlyCollection<TicketType> TicketTypes => _ticketTypes.AsReadOnly();

    public static TicketedEvent Create(string name, DateOnly startDay, DateOnly endDay,
        DateTime salesStartDateTime, DateTime salesEndDateTime)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Name cannot be empty.");

        if (endDay < startDay)
            throw new ValidationException("End day should be greater than start day.");

        if (salesStartDateTime >= salesEndDateTime)
            throw new ValidationException("Sales start time must be before sales end time.");

        if (salesEndDateTime > startDay.ToDateTime(TimeOnly.MinValue))
            throw new ValidationException("Sales must close before the event starts.");
        
        return new TicketedEvent(GetId(name), name, startDay, endDay, salesStartDateTime, salesEndDateTime);
    }
    
    public void AddTicketType(TicketType ticketType)
    {
        if (_ticketTypes.Any(existingTicketType => existingTicketType.Id == ticketType.Id))
        {
            throw new ValidationException("Ticket type already exists.");
        }
        
        if (ticketType.SessionStartDateTime < StartDay.ToDateTime(TimeOnly.MinValue) ||
            ticketType.SessionEndDateTime > EndDay.ToDateTime(TimeOnly.MaxValue))
        {
            throw new ValidationException("Ticket type session must be within the event duration.");
        }

        _ticketTypes.Add(ticketType);
    }

    public void RemoveTicketType(Guid ticketTypeId)
    {
        var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Id == ticketTypeId);
        if (ticketType is null) return;
        
        if (ticketType.HasTicketsReserved())
        {
            throw new ValidationException("Cannot remove ticket type with reserved tickets.");
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

    private static Guid GetId(string name)
    {
        return DeterministicGuidGenerator.Generate(name);
    }
}
