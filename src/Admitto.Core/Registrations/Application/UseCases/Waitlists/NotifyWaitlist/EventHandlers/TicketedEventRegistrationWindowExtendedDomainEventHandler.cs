using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.ProcessWaitlistNotifications;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.PromoteEntireWaitlist;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.NotifyWaitlist.EventHandlers;

/// <summary>
/// Handles <see cref="TicketedEventRegistrationWindowExtendedDomainEvent"/> by re-running the waitlist check for every
/// ticket type in WaitlistMode, just as raising capacity does: seats freed while registration was closed weren't
/// offered, so they go to the queue as soon as registration is open again. A ticket type whose capacity limit was
/// removed while registration was closed has room for everyone, so everyone still waiting on it is offered a seat.
/// </summary>
internal sealed class TicketedEventRegistrationWindowExtendedDomainEventHandler(
    IRegistrationsWriteStore writeStore,
    ICommandHandler<ProcessWaitlistNotificationsCommand> processWaitlistNotificationsHandler,
    ICommandHandler<PromoteEntireWaitlistCommand> promoteEntireWaitlistHandler)
    : IDomainEventHandler<TicketedEventRegistrationWindowExtendedDomainEvent>
{
    public async ValueTask HandleAsync(
        TicketedEventRegistrationWindowExtendedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var catalog = await writeStore.TicketCatalogs.FirstOrDefaultAsync(
            c => c.Id == domainEvent.TicketedEventId && c.TeamId == domainEvent.TeamId,
            cancellationToken);

        if (catalog is null)
            return;

        var ticketTypes = catalog.TicketTypes.ToList();
        foreach (var ticketType in ticketTypes)
        {
            if (ticketType.WaitlistMode)
            {
                await processWaitlistNotificationsHandler.HandleAsync(
                    new ProcessWaitlistNotificationsCommand(
                        domainEvent.TicketedEventId.Value,
                        domainEvent.TeamId.Value,
                        ticketType.Id.Value),
                    cancellationToken);
            }
            else if (ticketType.PublicCapacity is null)
            {
                // Does nothing unless the ticket type's waitlist still has people queued.
                await promoteEntireWaitlistHandler.HandleAsync(
                    new PromoteEntireWaitlistCommand(
                        domainEvent.TicketedEventId.Value,
                        domainEvent.TeamId.Value,
                        ticketType.Id.Value),
                    cancellationToken);
            }
        }
    }
}
