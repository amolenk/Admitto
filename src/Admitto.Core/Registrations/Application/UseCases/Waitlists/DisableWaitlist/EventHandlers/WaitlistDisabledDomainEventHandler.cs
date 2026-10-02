using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.DisableWaitlist.EventHandlers;

/// <summary>
/// Handles <see cref="WaitlistDisabledDomainEvent"/> by dispatching <see cref="DisableWaitlistCommand"/> in the same
/// unit of work as the ticket type update.
/// </summary>
internal sealed class WaitlistDisabledDomainEventHandler(
    ICommandHandler<DisableWaitlistCommand> disableWaitlistHandler)
    : IDomainEventHandler<WaitlistDisabledDomainEvent>
{
    public async ValueTask HandleAsync(
        WaitlistDisabledDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        await disableWaitlistHandler.HandleAsync(
            new DisableWaitlistCommand(
                domainEvent.TicketedEventId.Value,
                domainEvent.TeamId.Value,
                domainEvent.TicketTypeId.Value,
                domainEvent.FreedSlots),
            cancellationToken);
    }
}
