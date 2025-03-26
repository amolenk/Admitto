namespace Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

public record ResolvePendingRegistrationCommand(Guid RegistrationId, bool TicketsReserved) : ICommand
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
