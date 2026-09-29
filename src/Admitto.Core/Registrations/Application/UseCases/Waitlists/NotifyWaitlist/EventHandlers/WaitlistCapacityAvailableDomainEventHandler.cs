using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.ProcessWaitlistNotifications;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.NotifyWaitlist.EventHandlers;

/// <summary>
/// Handles <see cref="WaitlistCapacityAvailableDomainEvent"/> (tickets released, a waitlist hold released,
/// MaxCapacity raised or ReservedCapacity lowered while in WaitlistMode) by dispatching
/// <see cref="ProcessWaitlistNotificationsCommand"/> in the same unit of work, so the front of the queue is offered
/// the seats the catalog has available.
/// </summary>
internal sealed class WaitlistCapacityAvailableDomainEventHandler(
    ICommandHandler<ProcessWaitlistNotificationsCommand> processWaitlistNotificationsHandler)
    : IDomainEventHandler<WaitlistCapacityAvailableDomainEvent>
{
    public async ValueTask HandleAsync(
        WaitlistCapacityAvailableDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        await processWaitlistNotificationsHandler.HandleAsync(
            new ProcessWaitlistNotificationsCommand(
                domainEvent.TicketedEventId.Value,
                domainEvent.TeamId.Value,
                domainEvent.TicketTypeId.Value),
            cancellationToken);
    }
}
