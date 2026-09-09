using Amolenk.Admitto.Core.Email.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CancelUnreconfirmedRegistrations;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CancelUnreconfirmedRegistrations.EventHandlers;

internal sealed class ReconfirmAutoExpiredIntegrationEventHandler(
    ICommandHandler<CancelUnreconfirmedRegistrationsCommand> handler,
    [FromKeyedServices(RegistrationsModule.Key)] IInbox inbox)
    : IIntegrationEventHandler<ReconfirmAutoExpiredIntegrationEvent>
{
    public async ValueTask HandleAsync(
        ReconfirmAutoExpiredIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        if (!await inbox.TryMarkAsProcessedByAsync<ReconfirmAutoExpiredIntegrationEventHandler>(
                integrationEvent,
                cancellationToken))
        {
            return;
        }

        var command = new CancelUnreconfirmedRegistrationsCommand(
            integrationEvent.TeamId,
            integrationEvent.TicketedEventId,
            integrationEvent.RegistrationReferences);

        await handler.HandleAsync(command, cancellationToken);
    }
}
