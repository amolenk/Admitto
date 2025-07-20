namespace Amolenk.Admitto.Application.UseCases.Registrations.RejectRegistration;

public class RejectRegistrationHandler(IDomainContext context) : ICommandHandler<RejectRegistrationCommand>
{
    public async ValueTask HandleAsync(RejectRegistrationCommand command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();

        // var registration = await context.Registrations.GetRegistrationAsync(command.RegistrationId, cancellationToken);
        //
        // registration.Reject();
    }
}