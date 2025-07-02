using System.Security.Cryptography;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Exceptions;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents an event for which attendees can register.
/// </summary>
public class TicketedEvent : AggregateRoot
{
    private readonly List<TicketType> _ticketTypes = [];
    private readonly List<EmailTemplate> _emailTemplates = [];

    // EF Core constructor
    private TicketedEvent()
    {
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

        var id = TicketedEventId.FromTeamIdAndName(teamId, name);
        
        return new TicketedEvent(id, teamId, name, startTime, endTime, registrationStartTime, registrationEndTime);
    }
    
    public void AddTicketType(string name, string slotName, int maxCapacity)
    {
        var ticketType = TicketType.Create(name, slotName, maxCapacity);

        if (_ticketTypes.Contains(ticketType))
        {
            throw DomainError.TicketedEvent.TicketTypeAlreadyExists();
        }
        
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
    
    public bool HasAvailableCapacity(IEnumerable<TicketQuantity> tickets)
    {
        return tickets.All(tq => 
            _ticketTypes.Any(tt => tt.Id == tq.TicketTypeId.Value && tt.HasAvailableCapacity(tq.Quantity)));
    }

    public void ReserveTickets(AttendeeRegistrationId registrationId, IEnumerable<TicketQuantity> tickets)
    {
    }

    public void HoldTickets(AttendeeRegistrationId registrationId, IEnumerable<TicketQuantity> tickets)
    {
        var ticketList = tickets.ToList();
        
        // If there's no capacity, reject the reservation
        if (!HasAvailableCapacity(ticketList))
        {
            AddDomainEvent(new TicketsReservationRejectedDomainEvent(Id, registrationId));
            return;
        }

        foreach (var ticketQuantity in ticketList)
        {
            var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Id == ticketQuantity.TicketTypeId.Value);
            if (ticketType is null)
            {
                // TODO
                // throw DomainError.TicketedEvent.TicketTypeNotFound(ticketQuantity.TicketTypeId);
            }
            
            ticketType.ReserveTickets(ticketQuantity.Quantity);
        }

        var confirmationCode = GenerateConfirmationCode();
        
        // AddDomainEvent(new TicketsReservedDomainEvent(Id, registrationId, confirmationCode));
    }

    public void ConfirmTickets(AttendeeRegistrationId registrationId, string confirmationCode)
    {
        
    }
    
    public void CancelTickets(IEnumerable<TicketQuantity> tickets)
    {
        // foreach (var ticketTypeId in ticketOrder.TicketTypeIds)
        // {
        //     var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Id == ticketTypeId);
        //     ticketType?.CancelTicket(); // TODO Quantity
        // }
    }

    private static string GenerateConfirmationCode()
    {
        // Generates a random 6-digit code (000000-999999)
        var bytes = new byte[4];
        RandomNumberGenerator.Fill(bytes);
        
        var value = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF; // Ensure non-negative
        var code = value % 1_000_000;
        
        return code.ToString("D6");
    }
}
