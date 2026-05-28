using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations.EventHandlers;

/// <summary>
/// Registers an initial per-event reconfirm trigger when a ticketed event is
/// created with a policy already in place. We re-query Registrations for the
/// trigger spec rather than reading it off the event payload because the
/// integration event does not carry the (optional) policy snapshot.
/// </summary>
internal sealed class TicketedEventCreatedIntegrationEventHandler(
    IRegistrationsFacade registrationsFacade,
    ICommandHandler<ScheduleReconfirmationsCommand> handler)
    : IIntegrationEventHandler<TicketedEventCreatedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        TicketedEventCreatedIntegrationEvent integrationEvent,
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
