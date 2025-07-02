using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Registrations.StartRegistration;

public record RegisterAttendeeRequest(
    Guid TicketedEventId,
    string Email,
    string FirstName,
    string LastName,
    Dictionary<string, string> Details,
    Dictionary<Guid, int> Tickets)
{
    public AttendeeRegistration ToAttendeeRegistration()
    {
        var tickets = Tickets.ToDictionary(
            x => new TicketTypeId(x.Key), x => x.Value);
        
        var registration = AttendeeRegistration.Create(TicketedEventId, Email, FirstName, LastName, tickets);

        foreach (var detail in Details)
        {
            registration.AddAttendeeDetail(detail.Key, detail.Value);
        }

        return registration;
    }
}
