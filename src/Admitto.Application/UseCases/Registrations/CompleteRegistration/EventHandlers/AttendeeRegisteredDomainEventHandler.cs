using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Application.UseCases.Registrations.CompleteRegistration.EventHandlers;

/// <summary>
/// Represents an event handler that completes the registration after tickets have been reserved.
/// </summary>
public class AttendeeRegisteredDomainEventHandler(CompleteRegistrationHandler completeRegistrationHandler)
    : IEventualDomainEventHandler<AttendeeRegisteredDomainEvent>
{
    public async ValueTask HandleAsync(AttendeeRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var command = new CompleteRegistrationCommand(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            domainEvent.RegistrationId,
            domainEvent.Email,
            domainEvent.FirstName,
            domainEvent.LastName,
            domainEvent.AdditionalDetails,
            domainEvent.Tickets)
        {
            CommandId = DeterministicGuid.Create($"{domainEvent.DomainEventId}:{nameof(CompleteRegistrationCommand)}")
        };

        await completeRegistrationHandler.HandleAsync(command, cancellationToken);
    }
}