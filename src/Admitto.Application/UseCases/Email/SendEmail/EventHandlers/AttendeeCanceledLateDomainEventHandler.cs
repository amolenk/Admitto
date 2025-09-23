using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Application.UseCases.Email.SendEmail.EventHandlers;

/// <summary>
/// Represents a domain event handler that sends a Cancellation email when a registration is canceled late.
/// </summary>
public class AttendeeCanceledLateDomainEventHandler(SendEmailHandler sendEmailHandler)
    : IEventualDomainEventHandler<AttendeeCanceledLateDomainEvent>
{
    public async ValueTask HandleAsync(AttendeeCanceledLateDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var command = new SendEmailCommand(
            domainEvent.TicketedEventId,
            domainEvent.AttendeeId,
            WellKnownEmailType.Canceled)
        {
            CommandId = DeterministicGuid.Create($"{domainEvent.DomainEventId}:{nameof(SendEmailCommand)}")
        };

        await sendEmailHandler.HandleAsync(command, cancellationToken);
    }
}