namespace Amolenk.Admitto.Application.UseCases.Registrations.CompleteRegistration;

public class CompleteRegistrationHandler(IDomainContext context) : ICommandHandler<CompleteRegistrationCommand>
{
    public async ValueTask HandleAsync(CompleteRegistrationCommand command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();

        // var registration = await context.Registrations.GetRegistrationAsync(command.RegistrationId, cancellationToken);
        //
        // registration.Complete();
    }
}