using Amolenk.Admitto.Application.UseCases.Registrations.StartRegistration;

namespace Amolenk.Admitto.IntegrationTests.UseCases.Attendees.RegisterAttendee;

public class RegisterAttendeeRequestBuilder
{
    private Guid _ticketedEventId = Guid.NewGuid();
    private string _email = "test@example.com";
    private string _firstName = "John";
    private string _lastName = "Doe";
    private Dictionary<string, string> _details = new Dictionary<string, string>
        {
            { "Company", "Test Company" }
        };

    private Dictionary<Guid, int> _tickets = new Dictionary<Guid, int>
        {
            { Guid.NewGuid(), 1 }
        };

    public RegisterAttendeeRequestBuilder WithTicketedEventId(Guid id)
    {
        _ticketedEventId = id;
        return this;
    }

    public RegisterAttendeeRequestBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public RegisterAttendeeRequestBuilder WithFirstName(string firstName)
    {
        _firstName = firstName;
        return this;
    }

    public RegisterAttendeeRequestBuilder WithLastName(string lastName)
    {
        _lastName = lastName;
        return this;
    }

    public RegisterAttendeeRequestBuilder WithDetails(Dictionary<string, string> details)
    {
        _details = details;
        return this;
    }

    public RegisterAttendeeRequestBuilder WithTickets(Dictionary<Guid, int> tickets)
    {
        _tickets = tickets;
        return this;
    }

    public RegisterAttendeeRequest Build()
    {
        return new RegisterAttendeeRequest(_ticketedEventId, _email, _firstName, _lastName, _details, _tickets!);
    }
}