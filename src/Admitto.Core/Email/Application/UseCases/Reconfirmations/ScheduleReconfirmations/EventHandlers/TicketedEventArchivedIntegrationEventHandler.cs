using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations.EventHandlers;

/// <summary>
/// Removes the per-event reconfirm trigger when the ticketed event is
/// archived. Idempotent: no-op when no trigger exists.
/// </summary>
internal sealed class TicketedEventArchivedIntegrationEventHandler(ICommandHandler<ScheduleReconfirmationsCommand> handler)
    : IIntegrationEventHandler<TicketedEventArchivedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        TicketedEventArchivedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new ScheduleReconfirmationsCommand(
                integrationEvent.TicketedEventId,
                Spec: null),
            cancellationToken);
    }
}
