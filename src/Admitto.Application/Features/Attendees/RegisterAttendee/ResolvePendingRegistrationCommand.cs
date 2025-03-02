namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee;

public record ResolvePendingRegistrationCommand(Guid RegistrationId, bool TicketsReserved) : ICommand
{
    public Guid CommandId { get; init; } = Guid.NewGuid();
}
