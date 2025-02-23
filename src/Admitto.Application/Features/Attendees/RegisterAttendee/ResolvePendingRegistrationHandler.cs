using Amolenk.Admitto.Application.Dtos;
using Amolenk.Admitto.Application.Exceptions;

namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee;

/// <summary>
/// Confirm the pending registration for a ticketed event.
/// </summary>
public class ResolvePendingRegistrationHandler(IAttendeeRepository attendeeRepository)
    : IRequestHandler<FinalizeRegistrationCommand>
{
    public async Task Handle(FinalizeRegistrationCommand request, CancellationToken cancellationToken)
    {
        var attendeeResult = await attendeeRepository.GetByIdAsync(request.AttendeeId);
        if (attendeeResult is null) throw new AttendeeNotFoundException();

        attendeeResult.Aggregate.FinalizeRegistration(request.RegistrationId);

        await attendeeRepository.SaveChangesAsync(
            attendeeResult.Aggregate,
            attendeeResult.Etag,
            attendeeResult.Aggregate.GetDomainEvents().Select(OutboxMessage.FromDomainEvent));
    }
}
