using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.IntegrationTests.TestHelpers.Builders;

public class AttendeeRegistrationBuilder
{
    private TicketedEventId _ticketedEventId = Guid.NewGuid();
    private string _email = "johndoe@example.com";
    private string _firstName = "John";
    private string _lastName = "Doe";
    private IDictionary<TicketTypeId, int> _tickets = new Dictionary<TicketTypeId, int>();

    public AttendeeRegistrationBuilder WithTicketedEventId(TicketedEventId ticketedEventId)
    {
        _ticketedEventId = ticketedEventId;
        return this;
    }

    public AttendeeRegistrationBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public AttendeeRegistrationBuilder WithFirstName(string firstName)
    {
        _firstName = firstName;
        return this;
    }

    public AttendeeRegistrationBuilder WithLastName(string lastName)
    {
        _lastName = lastName;
        return this;
    }

    public AttendeeRegistrationBuilder WithTickets(IDictionary<TicketTypeId, int> tickets)
    {
        _tickets = tickets;
        return this;
    }

    public Registration Build()
    {
        return Registration.Create(_ticketedEventId, _email, _firstName, _lastName, _tickets);
    }
}