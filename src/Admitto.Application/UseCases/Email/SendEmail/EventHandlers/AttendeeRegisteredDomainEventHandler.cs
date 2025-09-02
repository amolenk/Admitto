using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Application.UseCases.Email.SendEmail.EventHandlers;

/// <summary>
/// Represents a domain event handler that sends a Ticket email when a registration is completed.
/// </summary>
public class AttendeeRegisteredDomainEventHandler(SendEmailHandler sendEmailHandler)
    : IEventualDomainEventHandler<AttendeeRegisteredDomainEvent>
{
    public async ValueTask HandleAsync(AttendeeRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var command = new SendEmailCommand(
            domainEvent.TicketedEventId,
            domainEvent.AttendeeId,
            WellKnownEmailType.Ticket)
        {
            CommandId = DeterministicGuid.Create($"{domainEvent.DomainEventId}:{nameof(SendEmailCommand)}")
        };

        await sendEmailHandler.HandleAsync(command, cancellationToken);
    }
}