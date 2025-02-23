using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee.EventHandlers;

/// <summary>
/// Sends a rejection email when the ticket claim is rejected.
/// The command ID is generated deterministically based on the domain event ID to ensure idempotency.
/// </summary>
public class RegistrationRejectedDomainEventHandler(ISender mediator)
    : INotificationHandler<RegistrationRejectedDomainEvent>
{
    public Task Handle(RegistrationRejectedDomainEvent notification, CancellationToken cancellationToken)
    {
        return mediator.Send(
            new SendRejectionEmailCommand(notification.AttendeeId, notification.RegistrationId)
            {
                CommandId = DeterministicGuidGenerator.Generate($"{notification.DomainEventId}:SendRejectionEmail")
            },
            cancellationToken);
    }
}
