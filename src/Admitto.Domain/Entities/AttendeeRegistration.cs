using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Exceptions;
using Amolenk.Admitto.Domain.Utilities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents the registration for an event.
/// </summary>
// TODO Rename to Registration
public class AttendeeRegistration : AggregateRoot
{
    private readonly List<AttendeeDetail> _details = [];
    private readonly List<TicketQuantity> _tickets = [];
    
    private AttendeeRegistration()
    {
    }

    private AttendeeRegistration(AttendeeRegistrationId id, TicketedEventId ticketedEventId, string email, 
        string firstName, string lastName, AttendeeRegistrationStatus status)
        : base(id.Value)
    {
        TicketedEventId = ticketedEventId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Status = status;
    }

    public TicketedEventId TicketedEventId { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public AttendeeRegistrationStatus Status { get; private set; }
    public IReadOnlyCollection<AttendeeDetail> Details => _details.AsReadOnly();
    public IReadOnlyCollection<TicketQuantity> Tickets => _tickets.AsReadOnly();
    
    public static AttendeeRegistration Create(TicketedEventId ticketedEventId, string email, string firstName, 
        string lastName, IDictionary<TicketTypeId, int> tickets)
    {
        var attendeeId = AttendeeId.FromEmail(email);
        var attendeeRegistrationId = AttendeeRegistrationId.FromAttendeeAndEvent(attendeeId, ticketedEventId);
        
        var registration = new AttendeeRegistration(attendeeRegistrationId, ticketedEventId, email, firstName,
            lastName, AttendeeRegistrationStatus.Pending);

        foreach (var ticket in tickets)
        {
            registration.AddTicket(ticket.Key, ticket.Value);
        }

        return registration;
    }
    
    public void AddTicket(TicketTypeId ticketType, int quantity)
    {
        if (_tickets.Any(t => t.TicketTypeId == ticketType))
        {
            throw DomainError.AttendeeRegistration.TicketTypeAlreadyExists();
        }

        _tickets.Add(new TicketQuantity(ticketType, quantity));
    }
    
    public void AddAttendeeDetail(string name, string value)
    {
        var attendeeDetail = new AttendeeDetail(name, value);
 
        if (_details.Contains(attendeeDetail))
        {
            throw new DomainException($"Attendee detail with name {name} already exists.");
        }
        
        _details.Add(new AttendeeDetail(name, value));
    }

    public void Complete()
    {
        // // TODO other edge cases?
        // Status = AttendeeRegistrationStatus.Accepted;
        //
        // AddDomainEvent(new RegistrationAcceptedDomainEvent(
        //     DeterministicGuidGenerator.Generate(Email), Id));
    }
}
