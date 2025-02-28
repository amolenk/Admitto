namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee;

/// <summary>
/// Accept or reject the pending registration for a ticketed event.
/// </summary>
public class ResolvePendingRegistrationHandler(IAttendeeRepository attendeeRepository)
    : ICommandHandler<ResolvePendingRegistrationCommand>
{
    public async ValueTask HandleAsync(ResolvePendingRegistrationCommand command, CancellationToken cancellationToken)
    {
        var (attendee, etag) = await attendeeRepository.GetByIdAsync(command.AttendeeId);

        // Update the pending registration.
        if (command.TicketsReserved)
        {
            attendee.AcceptPendingRegistration(command.RegistrationId);
        }
        else
        {
            attendee.RejectPendingReservation(command.RegistrationId);
        }
        
        await attendeeRepository.SaveChangesAsync(
            attendee,
            etag,
            attendee.GetDomainEvents().Select(OutboxMessage.FromDomainEvent));
    }
}
