using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlist.ProcessWaitlistNotifications;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlist.NotifyWaitlist.EventHandlers;

/// <summary>
/// Handles <see cref="RegistrationCancelledDomainEvent"/> for the waitlist notification flow.
/// For each ticket type in the cancelled registration that has an active waitlist,
/// dispatches <see cref="ProcessWaitlistNotificationsCommand"/> with the number of freed slots.
/// </summary>
internal sealed class RegistrationCancelledDomainEventHandler(
    IRegistrationsWriteStore writeStore,
    ICommandHandler<ProcessWaitlistNotificationsCommand> processWaitlistNotificationsHandler)
    : IDomainEventHandler<RegistrationCancelledDomainEvent>
{
    public async ValueTask HandleAsync(
        RegistrationCancelledDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var registration = await writeStore.Registrations
            .GetAsync(r => r.Id == domainEvent.RegistrationId, cancellationToken);

        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(tc => tc.Id == domainEvent.TicketedEventId, cancellationToken);

        if (catalog is null)
            return;

        // Group the freed tickets by ticket type to compute how many slots each type gained.
        var freedSlotsByType = registration.Tickets
            .GroupBy(t => t.Id)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var (ticketTypeId, freedSlots) in freedSlotsByType)
        {
            var ticketType = catalog.GetTicketType(ticketTypeId);
            if (ticketType is null || !ticketType.WaitlistEnabled || !ticketType.WaitlistMode)
                continue;

            await processWaitlistNotificationsHandler.HandleAsync(
                new ProcessWaitlistNotificationsCommand(domainEvent.TicketedEventId.Value, ticketTypeId.Value, freedSlots),
                cancellationToken);
        }
    }
}
