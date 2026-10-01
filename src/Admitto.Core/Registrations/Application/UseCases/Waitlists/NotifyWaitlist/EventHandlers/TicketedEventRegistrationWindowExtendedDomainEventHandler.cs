using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.ProcessWaitlistNotifications;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.NotifyWaitlist.EventHandlers;

/// <summary>
/// Handles <see cref="TicketedEventRegistrationWindowExtendedDomainEvent"/> by re-running the waitlist check for every
/// ticket type in WaitlistMode, just as raising capacity does: seats freed while registration was closed weren't
/// offered, so they go to the queue as soon as registration is open again.
/// </summary>
internal sealed class TicketedEventRegistrationWindowExtendedDomainEventHandler(
    IRegistrationsWriteStore writeStore,
    ICommandHandler<ProcessWaitlistNotificationsCommand> processWaitlistNotificationsHandler)
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

        var ticketTypeIds = catalog.TicketTypes
            .Where(t => t.WaitlistMode)
            .Select(t => t.Id)
            .ToList();

        foreach (var ticketTypeId in ticketTypeIds)
        {
            await processWaitlistNotificationsHandler.HandleAsync(
                new ProcessWaitlistNotificationsCommand(
                    domainEvent.TicketedEventId.Value,
                    domainEvent.TeamId.Value,
                    ticketTypeId.Value),
                cancellationToken);
        }
    }
}
