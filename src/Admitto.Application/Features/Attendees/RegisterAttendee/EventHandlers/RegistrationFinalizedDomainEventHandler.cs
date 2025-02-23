using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee.EventHandlers;

/// <summary>
/// Sends a confirmation email when the registration is confirmed.
/// The command ID is generated deterministically based on the domain event ID to ensure idempotency.
/// </summary>
public class RegistrationFinalizedDomainEventHandler(ISender mediator)
    : INotificationHandler<RegistrationFinalizedDomainEvent>
{
    public Task Handle(RegistrationFinalizedDomainEvent notification, CancellationToken cancellationToken)
    {
        return mediator.Send(
            new SendAcceptanceEmailCommand(notification.AttendeeId, notification.RegistrationId)
            {
                CommandId = DeterministicGuidGenerator.Generate($"{notification.DomainEventId}:SendConfirmationEmail")
            },
            cancellationToken);
    }
}