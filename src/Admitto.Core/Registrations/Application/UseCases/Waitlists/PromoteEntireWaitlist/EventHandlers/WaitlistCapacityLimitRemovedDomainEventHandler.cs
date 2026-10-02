using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.PromoteEntireWaitlist.EventHandlers;

/// <summary>
/// Handles <see cref="WaitlistCapacityLimitRemovedDomainEvent"/> by dispatching
/// <see cref="PromoteEntireWaitlistCommand"/> in the same unit of work as the ticket type update.
/// </summary>
internal sealed class WaitlistCapacityLimitRemovedDomainEventHandler(
    ICommandHandler<PromoteEntireWaitlistCommand> promoteEntireWaitlistHandler)
    : IDomainEventHandler<WaitlistCapacityLimitRemovedDomainEvent>
{
    public async ValueTask HandleAsync(
        WaitlistCapacityLimitRemovedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        await promoteEntireWaitlistHandler.HandleAsync(
            new PromoteEntireWaitlistCommand(
                domainEvent.TicketedEventId.Value,
                domainEvent.TeamId.Value,
                domainEvent.TicketTypeId.Value),
            cancellationToken);
    }
}
