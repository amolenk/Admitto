namespace Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

/// <summary>
/// Accept or reject the pending registration for a ticketed event.
/// </summary>
public class ResolvePendingRegistrationHandler(IDomainContext context)
    : ICommandHandler<ResolvePendingRegistrationCommand>
{
    public async ValueTask HandleAsync(ResolvePendingRegistrationCommand command, CancellationToken cancellationToken)
    {
        var registration = await context.AttendeeRegistrations.GetByIdAsync(command.RegistrationId, cancellationToken);

        if (command.TicketsReserved)
        {
            registration.Accept();

            context.AttendeeRegistrations.Update(registration);
        }
        else
        {
            context.AttendeeRegistrations.Remove(registration);
        }
    }
}
