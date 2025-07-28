using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Utilities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Email.SendEmail.EventHandlers;

/// <summary>
/// Represents a domain event handler that sends a Ticket email when a registration is completed.
/// </summary>
public class RegistrationCompletedDomainEventHandler(SendEmailHandler sendEmailHandler)
    : IEventualDomainEventHandler<RegistrationCompletedDomainEvent>
{
    public async ValueTask HandleAsync(RegistrationCompletedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var command = new SendEmailCommand(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            domainEvent.AttendeeId,
            EmailType.Ticket)
        {
            CommandId = DeterministicGuid.Create($"{domainEvent.DomainEventId}:{nameof(SendEmailCommand)}")
        };

        await sendEmailHandler.HandleAsync(command, cancellationToken);
    }
}