using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations.EventHandlers;

/// <summary>
/// Creates, replaces, or removes the per-event reconfirm trigger in response
/// to policy changes from the Registrations module. When the published policy
/// snapshot is <c>null</c> (policy cleared), the trigger is removed; otherwise
/// the trigger is upserted using the current event time zone (looked up via
/// the facade since the integration event does not carry the time zone).
/// </summary>
[RequiresCapability(HostCapability.Jobs | HostCapability.Email)]
internal sealed class TicketedEventReconfirmPolicyChangedIntegrationEventHandler(
    IRegistrationsFacade registrationsFacade,
    IMediator mediator)
    : IIntegrationEventHandler<TicketedEventReconfirmPolicyChangedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        TicketedEventReconfirmPolicyChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var ticketedEventId = TicketedEventId.From(integrationEvent.TicketedEventId);

        if (integrationEvent.Policy is null)
        {
            await mediator.SendAsync(
                new ScheduleReconfirmationsCommand(ticketedEventId, Spec: null),
                cancellationToken);
            return;
        }

        // Re-query to pick up the current TimeZone (and to confirm the event
        // is still Active and the policy has not been re-cleared concurrently).
        var spec = await registrationsFacade.GetReconfirmTriggerSpecAsync(
            ticketedEventId, cancellationToken);

        await mediator.SendAsync(
            new ScheduleReconfirmationsCommand(ticketedEventId, spec),
            cancellationToken);
    }
}
