using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee;

public record ReserveTicketsCommand(Guid RegistrationId) : ICommand
{
    public Guid CommandId { get; init; } = Guid.NewGuid();
}
