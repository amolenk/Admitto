using Amolenk.Admitto.Domain.Exceptions;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents an event for which attendees can register.
/// </summary>
public class TicketedEvent : AggregateRoot
{
    private readonly List<TicketType> _ticketTypes;
    private readonly List<EmailTemplate> _emailTemplates;

    // EF Core constructor
    private TicketedEvent()
    {
        _ticketTypes = [];
        _emailTemplates = [];
    }
    
    private TicketedEvent(TicketedEventId id, TeamId teamId, string name, DateTimeOffset startTime,
        DateTimeOffset endTime, DateTimeOffset registrationStartTime, DateTimeOffset registrationEndTime)
        : base(id)
    {
        TeamId = teamId;
        Name = name;
        StartTime = startTime;
        EndTime = endTime;
        RegistrationStartTime = registrationStartTime;
        RegistrationEndTime = registrationEndTime;
        _ticketTypes = [];
        _emailTemplates = [];
    }

    public Guid TeamId { get; private set; }
    public string Name { get; private set; } = null!;
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset EndTime { get; private set; }
    public DateTimeOffset RegistrationStartTime { get; private set; }
    public DateTimeOffset RegistrationEndTime { get; private set; }
    public IReadOnlyCollection<TicketType> TicketTypes => _ticketTypes.AsReadOnly();

    public IReadOnlyCollection<EmailTemplate> EmailTemplates => _emailTemplates.AsReadOnly();

    public static TicketedEvent Create(TeamId teamId, string name, DateTimeOffset startTime, DateTimeOffset endTime,
        DateTimeOffset registrationStartTime, DateTimeOffset registrationEndTime)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name cannot be empty.");

        if (endTime < startTime)
            throw new DomainException("End day should be greater than start day.");

        if (registrationStartTime >= registrationEndTime)
            throw new DomainException("Sales start time must be before sales end time.");

        if (registrationEndTime > startTime)
            throw new DomainException("Registration must close before the event starts.");

        var id = TicketedEventId.FromEventName(name);
        
        return new TicketedEvent(id, teamId, name, startTime, endTime, registrationStartTime, registrationEndTime);
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

    public void SetEmailTemplate(EmailTemplateId templateId, EmailTemplate template)
    {
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
