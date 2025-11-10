using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Utilities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Email.SendEmail.EventHandlers;

/// <summary>
/// Represents a domain event handler that sends a Cancellation email when a registration is canceled.
/// </summary>
public class AttendeeCanceledDomainEventHandler(SendEmailHandler sendEmailHandler)
    : IEventualDomainEventHandler<AttendeeCanceledDomainEvent>
{
    public async ValueTask HandleAsync(AttendeeCanceledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var emailType = domainEvent.Reason == CancellationReason.VisaLetterDenied
            ? WellKnownEmailType.VisaLetterDenied
            : WellKnownEmailType.Canceled;
        
        var command = new SendEmailCommand(
            domainEvent.TicketedEventId,
            domainEvent.AttendeeId,
            emailType)
        {
            CommandId = DeterministicGuid.Create($"{domainEvent.DomainEventId}:{nameof(SendEmailCommand)}")
        };

        await sendEmailHandler.HandleAsync(command, cancellationToken);
    }
}