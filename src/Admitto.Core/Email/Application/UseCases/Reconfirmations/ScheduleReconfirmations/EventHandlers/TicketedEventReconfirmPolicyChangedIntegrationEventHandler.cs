using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations.EventHandlers;

/// <summary>
/// Creates, replaces, or removes the per-event reconfirm trigger in response
/// to policy changes from the Registrations module. When the published policy
/// snapshot is <c>null</c> (policy cleared), the trigger is removed; otherwise
/// the trigger is upserted using the current event time zone (looked up via
/// the facade since the integration event does not carry the time zone).
/// </summary>
internal sealed class TicketedEventReconfirmPolicyChangedIntegrationEventHandler(
    IRegistrationsFacade registrationsFacade,
    ICommandHandler<ScheduleReconfirmationsCommand> handler)
    : IIntegrationEventHandler<TicketedEventReconfirmPolicyChangedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        TicketedEventReconfirmPolicyChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var ticketedEventId = TicketedEventId.From(integrationEvent.TicketedEventId);

        if (integrationEvent.Policy is null)
        {
            await handler.HandleAsync(
                new ScheduleReconfirmationsCommand(integrationEvent.TicketedEventId, Spec: null),
                cancellationToken);
            return;
        }

        // Re-query to pick up the current TimeZone (and to confirm the event
        // is still Active and the policy has not been re-cleared concurrently).
        var spec = await registrationsFacade.GetReconfirmTriggerSpecAsync(
            ticketedEventId.Value, cancellationToken);

        await handler.HandleAsync(
            new ScheduleReconfirmationsCommand(integrationEvent.TicketedEventId, spec),
            cancellationToken);
    }
}
