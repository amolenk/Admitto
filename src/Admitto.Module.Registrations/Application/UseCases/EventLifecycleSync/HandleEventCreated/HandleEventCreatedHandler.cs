using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Application.Services;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.EventLifecycleSync.HandleEventCreated;

/// <summary>
/// Creates the <see cref="Domain.Entities.TicketedEventLifecycleGuard"/> for a newly-created
/// ticketed event. Idempotent: re-delivery is a no-op when the guard already exists.
/// </summary>
internal sealed class HandleEventCreatedHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<HandleEventCreatedCommand>
{
    public async ValueTask HandleAsync(HandleEventCreatedCommand command, CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.TicketedEventId);

        await LifecycleGuardStore.LoadOrCreateAsync(writeStore, eventId, cancellationToken);
    }
}
