namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee;

public record ResolvePendingRegistrationCommand(Guid RegistrationId, Guid AttendeeId, bool TicketsReserved) : ICommand
{
    public Guid CommandId { get; init; } = Guid.NewGuid();
}
