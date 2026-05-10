using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Core.Registrations.Application.Messaging.EventHandlers;

internal sealed class OtpCodeRequestedDomainEventHandler(
    [FromKeyedServices(RegistrationsModule.Key)] IOutbox outbox)
    : IDomainEventHandler<OtpCodeRequestedDomainEvent>
{
    public ValueTask HandleAsync(OtpCodeRequestedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new OtpCodeRequestedIntegrationEvent(
            domainEvent.OtpCodeId.Value,
            domainEvent.TeamId.Value,
            domainEvent.TicketedEventId.Value,
            domainEvent.EventName,
            domainEvent.RecipientEmail.Value,
            domainEvent.PlainCode));

        return ValueTask.CompletedTask;
    }
}
