using Amolenk.Admitto.Module.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations.EventHandlers;

/// <summary>
/// Removes the per-event reconfirm trigger when the ticketed event is
/// cancelled. Idempotent: no-op when no trigger exists.
/// </summary>
[RequiresCapability(HostCapability.Jobs | HostCapability.Email)]
internal sealed class TicketedEventCancelledReconfirmIntegrationEventHandler(IMediator mediator)
    : IIntegrationEventHandler<TicketedEventCancelled>
{
    public async ValueTask HandleAsync(
        TicketedEventCancelled integrationEvent,
        CancellationToken cancellationToken)
    {
        await mediator.SendAsync(
            new ScheduleReconfirmationsCommand(
                TicketedEventId.From(integrationEvent.TicketedEventId),
                Spec: null),
            cancellationToken);
    }
}
