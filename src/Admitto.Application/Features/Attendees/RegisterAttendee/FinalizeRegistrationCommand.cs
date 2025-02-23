namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee;

public record FinalizeRegistrationCommand(Guid RegistrationId, Guid AttendeeId, Guid TicketedEventId) : ICommand
{
    public Guid CommandId { get; init; } = Guid.NewGuid();
}
