using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations.EventHandlers;

/// <summary>
/// Atomically replaces the per-event reconfirm trigger when the event's IANA
/// time zone changes, so the cron continues to fire at the same local hour.
/// No-ops when the event has no active reconfirm policy.
/// </summary>
[RequiresCapability(HostCapability.Jobs | HostCapability.Email)]
internal sealed class TicketedEventTimeZoneChangedIntegrationEventHandler(
    IRegistrationsFacade registrationsFacade,
    IMediator mediator)
    : IIntegrationEventHandler<TicketedEventTimeZoneChangedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        TicketedEventTimeZoneChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var ticketedEventId = TicketedEventId.From(integrationEvent.TicketedEventId);

        var spec = await registrationsFacade.GetReconfirmTriggerSpecAsync(
            ticketedEventId, cancellationToken);

        if (spec is null)
            return;

        await mediator.SendAsync(
            new ScheduleReconfirmationsCommand(ticketedEventId, spec),
            cancellationToken);
    }
}
