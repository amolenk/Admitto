using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations.EventHandlers;

/// <summary>
/// Removes the per-event reconfirm trigger when the ticketed event is
/// cancelled. Idempotent: no-op when no trigger exists.
/// </summary>
internal sealed class TicketedEventCancelledIntegrationEventHandler(ICommandHandler<ScheduleReconfirmationsCommand> handler)
    : IIntegrationEventHandler<TicketedEventCancelledIntegrationEvent>
{
    public async ValueTask HandleAsync(
        TicketedEventCancelledIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new ScheduleReconfirmationsCommand(
                integrationEvent.TicketedEventId,
                Spec: null),
            cancellationToken);
    }
}
