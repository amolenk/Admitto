using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations.EventHandlers;

/// <summary>
/// Atomically replaces the per-event reconfirm trigger when the event's IANA
/// time zone changes, so the cron continues to fire at the same local hour.
/// No-ops when the event has no active reconfirm policy.
/// </summary>
internal sealed class TicketedEventTimeZoneChangedIntegrationEventHandler(
    IRegistrationsFacade registrationsFacade,
    ICommandHandler<ScheduleReconfirmationsCommand> handler)
    : IIntegrationEventHandler<TicketedEventTimeZoneChangedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        TicketedEventTimeZoneChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var ticketedEventId = TicketedEventId.From(integrationEvent.TicketedEventId);

        var spec = await registrationsFacade.GetReconfirmTriggerSpecAsync(
            ticketedEventId.Value, cancellationToken);

        if (spec is null)
            return;

        await handler.HandleAsync(
            new ScheduleReconfirmationsCommand(integrationEvent.TicketedEventId, spec),
            cancellationToken);
    }
}
