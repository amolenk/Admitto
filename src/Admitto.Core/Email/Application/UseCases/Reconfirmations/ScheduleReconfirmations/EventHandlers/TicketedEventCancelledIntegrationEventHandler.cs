using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations.EventHandlers;

/// <summary>
/// Removes the per-event reconfirm trigger when the ticketed event is
/// cancelled. Idempotent: no-op when no trigger exists.
/// </summary>
[RequiresCapability(HostCapability.Jobs | HostCapability.Email)]
internal sealed class TicketedEventCancelledIntegrationEventHandler(IMediator mediator)
    : IIntegrationEventHandler<TicketedEventCancelledIntegrationEvent>
{
    public async ValueTask HandleAsync(
        TicketedEventCancelledIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        await mediator.SendAsync(
            new ScheduleReconfirmationsCommand(
                integrationEvent.TicketedEventId,
                Spec: null),
            cancellationToken);
    }
}
