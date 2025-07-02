namespace Amolenk.Admitto.Application.UseCases.Registrations.CompleteRegistration;

public class CompleteRegistrationHandler(IDomainContext context) : ICommandHandler<CompleteRegistrationCommand>
{
    public async ValueTask HandleAsync(CompleteRegistrationCommand command, CancellationToken cancellationToken)
    {
        var registration = await context.AttendeeRegistrations.FindAsync([command.RegistrationId], cancellationToken);
        if (registration is null)
        {
            throw ValidationError.AttendeeRegistration.NotFound(command.RegistrationId);
        }

        registration.Complete();
    }
}