using System.Text.Json.Serialization;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Utilities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents the registration for an event.
/// </summary>
public class AttendeeRegistration : AggregateRoot
{
    [JsonConstructor]    
    private AttendeeRegistration(Guid id, Guid ticketedEventId, string email, string firstName,
        string lastName, string organizationName, TicketOrder ticketOrder, AttendeeRegistrationStatus status)
        : base(id)
    {
        TicketedEventId = ticketedEventId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        OrganizationName = organizationName;
        TicketOrder = ticketOrder;
        Status = status;
    }

    public Guid TicketedEventId { get; private set; }
    public string Email { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string OrganizationName { get; private set; }
    public TicketOrder TicketOrder { get; private set; }
    public AttendeeRegistrationStatus Status { get; private set; }

    public static Guid GetId(string email, Guid ticketedEventId)
    {
        return DeterministicGuidGenerator.Generate($"{email}:{ticketedEventId}");
    }

    public static AttendeeRegistration Create(Guid ticketedEventId, string email, string firstName,
        string lastName, string organizationName, TicketOrder ticketOrder)
    {
        return new AttendeeRegistration(GetId(email, ticketedEventId), ticketedEventId, email, firstName,
            lastName, organizationName, ticketOrder, AttendeeRegistrationStatus.Pending);
    }

    public void Accept()
    {
        // TODO other edge cases?
        Status = AttendeeRegistrationStatus.Accepted;
        
        AddDomainEvent(new RegistrationAcceptedDomainEvent(
            DeterministicGuidGenerator.Generate(Email), Id));
    }
}
