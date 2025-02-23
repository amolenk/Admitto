using Amolenk.Admitto.Application.Dtos;
using Amolenk.Admitto.Application.Exceptions;

namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee;

/// <summary>
/// Rejects an unconfirmed registration. This can happen when the tickets run out during the
/// registration flow.
/// </summary>
public class RejectRegistrationHandler(IAttendeeRepository attendeeRepository)
    : IRequestHandler<RejectRegistrationCommand>
{
    public async Task Handle(RejectRegistrationCommand request, CancellationToken cancellationToken)
    {
        var attendeeResult = await attendeeRepository.GetByIdAsync(request.AttendeeId);
        if (attendeeResult is null) throw new AttendeeNotFoundException();

        attendeeResult.Aggregate.RejectReservation(request.RegistrationId);

        await attendeeRepository.SaveChangesAsync(
            attendeeResult.Aggregate,
            attendeeResult.Etag,
            attendeeResult.Aggregate.GetDomainEvents().Select(OutboxMessage.FromDomainEvent));
    }
}
