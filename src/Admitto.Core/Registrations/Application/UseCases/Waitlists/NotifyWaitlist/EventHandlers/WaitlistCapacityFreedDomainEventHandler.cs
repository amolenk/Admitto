using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.ProcessWaitlistNotifications;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.NotifyWaitlist.EventHandlers;

/// <summary>
/// Handles <see cref="WaitlistCapacityFreedDomainEvent"/> (raising MaxCapacity or lowering ReservedCapacity while
/// in WaitlistMode) by dispatching <see cref="ProcessWaitlistNotificationsCommand"/> with the freed slots, so the
/// front of the queue is offered a coupon for each new slot.
/// </summary>
internal sealed class WaitlistCapacityFreedDomainEventHandler(
    ICommandHandler<ProcessWaitlistNotificationsCommand> processWaitlistNotificationsHandler)
    : IDomainEventHandler<WaitlistCapacityFreedDomainEvent>
{
    public async ValueTask HandleAsync(
        WaitlistCapacityFreedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        await processWaitlistNotificationsHandler.HandleAsync(
            new ProcessWaitlistNotificationsCommand(
                domainEvent.TicketedEventId.Value,
                domainEvent.TeamId.Value,
                domainEvent.TicketTypeId.Value,
                domainEvent.FreedSlots),
            cancellationToken);
    }
}
