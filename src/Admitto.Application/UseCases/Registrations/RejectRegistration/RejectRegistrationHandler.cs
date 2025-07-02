using Amolenk.Admitto.Application.UseCases.Registrations.CompleteRegistration;

namespace Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

public class RejectRegistrationHandler : ICommandHandler<CompleteRegistrationCommand>
{
    public ValueTask HandleAsync(CompleteRegistrationCommand command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}