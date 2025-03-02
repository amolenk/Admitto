namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee;

/// <summary>
/// Accept or reject the pending registration for a ticketed event.
/// </summary>
public class ResolvePendingRegistrationHandler(IAttendeeRegistrationRepository repository)
    : ICommandHandler<ResolvePendingRegistrationCommand>
{
    public async ValueTask HandleAsync(ResolvePendingRegistrationCommand command, CancellationToken cancellationToken)
    {
        var (registration, etag) = await repository.GetByIdAsync(command.RegistrationId);

        if (command.TicketsReserved)
        {
            registration.Accept();

            await repository.SaveChangesAsync(
                registration,
                etag,
                registration.GetDomainEvents().Select(OutboxMessage.FromDomainEvent));
        }
        else
        {
            await repository.DeleteAsync(registration.Id);
        }
    }
}
