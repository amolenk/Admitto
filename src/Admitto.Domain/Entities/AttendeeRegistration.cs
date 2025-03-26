using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Utilities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents the registration for an event.
/// </summary>
public class AttendeeRegistration : AggregateRoot
{
    private AttendeeRegistration()
    {
    }

    private AttendeeRegistration(AttendeeRegistrationId id, TicketedEventId ticketedEventId, string email,
        string firstName, string lastName, string organizationName, TicketOrder ticketOrder,
        AttendeeRegistrationStatus status)
        : base(id.Value)
    {
        TicketedEventId = ticketedEventId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        OrganizationName = organizationName;
        TicketOrder = ticketOrder;
        Status = status;
    }

    public TicketedEventId TicketedEventId { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string OrganizationName { get; private set; } = null!;
    public TicketOrder TicketOrder { get; private set; } = null!;
    public AttendeeRegistrationStatus Status { get; private set; }

    public static AttendeeRegistration Create(TicketedEventId ticketedEventId, string email, string firstName,
        string lastName, string organizationName, TicketOrder ticketOrder)
    {
        var attendeeId = AttendeeId.FromEmail(email);
        var attendeeRegistrationId = AttendeeRegistrationId.FromAttendeeAndEvent(attendeeId, ticketedEventId);
        
        return new AttendeeRegistration(attendeeRegistrationId, ticketedEventId, email, firstName,
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
