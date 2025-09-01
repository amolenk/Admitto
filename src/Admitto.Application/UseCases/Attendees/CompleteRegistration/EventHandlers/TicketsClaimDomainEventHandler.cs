using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Application.UseCases.Attendees.CompleteRegistration.EventHandlers;

/// <summary>
/// When tickets are claimed, this handler initiates the completion of the registration process.
/// </summary>
public class TicketsClaimDomainEventHandler(CompleteRegistrationHandler completeRegistrationHandler)
    : IEventualDomainEventHandler<TicketsClaimedDomainEvent>
{
    public async ValueTask HandleAsync(TicketsClaimedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var command = new CompleteRegistrationCommand(
            domainEvent.TicketedEventId,
            domainEvent.ParticipantId,
            domainEvent.Email,
            domainEvent.FirstName,
            domainEvent.LastName,
            domainEvent.AdditionalDetails,
            domainEvent.ClaimedTickets)
        {
            CommandId = DeterministicGuid.Create($"{domainEvent.DomainEventId}:{nameof(CompleteRegistrationCommand)}")
        };

        await completeRegistrationHandler.HandleAsync(command, cancellationToken);
    }
}