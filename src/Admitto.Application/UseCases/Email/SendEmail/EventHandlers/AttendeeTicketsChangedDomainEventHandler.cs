using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Application.UseCases.Email.SendEmail.EventHandlers;

/// <summary>
/// Represents a domain event handler that re-sends a Ticket email when the tickets of a registration have changed.
/// </summary>
public class AttendeeTicketsChangedDomainEventHandler(SendEmailHandler sendEmailHandler)
    : IEventualDomainEventHandler<AttendeeTicketsChangedDomainEvent>
{
    public async ValueTask HandleAsync(AttendeeTicketsChangedDomainEvent domainEvent, CancellationToken cancellationToken)
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