using Amolenk.Admitto.Core.Module.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations.EventHandlers;

/// <summary>
/// Removes the per-event reconfirm trigger when the ticketed event is
/// archived. Idempotent: no-op when no trigger exists.
/// </summary>
[RequiresCapability(HostCapability.Jobs | HostCapability.Email)]
internal sealed class TicketedEventArchivedIntegrationEventHandler(IMediator mediator)
    : IIntegrationEventHandler<TicketedEventArchived>
{
    public async ValueTask HandleAsync(
        TicketedEventArchived integrationEvent,
        CancellationToken cancellationToken)
    {
        await mediator.SendAsync(
            new ScheduleReconfirmationsCommand(
                integrationEvent.TicketedEventId,
                Spec: null),
            cancellationToken);
    }
}
