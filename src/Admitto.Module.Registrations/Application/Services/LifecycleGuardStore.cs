using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.Services;

/// <summary>
/// Helper for loading or creating the lifecycle guard for an event.
/// </summary>
internal static class LifecycleGuardStore
{
    /// <summary>
    /// Loads the guard for the given event, or creates a new Active guard if none exists.
    /// </summary>
    public static async ValueTask<TicketedEventLifecycleGuard> LoadOrCreateAsync(
        IRegistrationsWriteStore writeStore,
        TicketedEventId eventId,
        CancellationToken cancellationToken)
    {
        var guard = await writeStore.TicketedEventLifecycleGuards
            .FirstOrDefaultAsync(g => g.Id == eventId, cancellationToken);

        if (guard is null)
        {
            guard = TicketedEventLifecycleGuard.Create(eventId);
            await writeStore.TicketedEventLifecycleGuards.AddAsync(guard, cancellationToken);
        }

        return guard;
    }
}
