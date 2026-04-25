using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations.EventHandlers;

/// <summary>
/// Registers an initial per-event reconfirm trigger when a ticketed event is
/// created with a policy already in place. We re-query Registrations for the
/// trigger spec rather than reading it off the event payload because the
/// integration event does not carry the (optional) policy snapshot.
/// </summary>
[RequiresCapability(HostCapability.Jobs | HostCapability.Email)]
internal sealed class TicketedEventCreatedIntegrationEventHandler(
    IRegistrationsFacade registrationsFacade,
    IMediator mediator)
    : IIntegrationEventHandler<TicketedEventCreated>
{
    public async ValueTask HandleAsync(
        TicketedEventCreated integrationEvent,
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
