using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Application.Services;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.EventLifecycleSync.HandleEventCancelled;

internal sealed class HandleEventCancelledHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<HandleEventCancelledCommand>
{
    public async ValueTask HandleAsync(HandleEventCancelledCommand command, CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.TicketedEventId);

        var guard = await LifecycleGuardStore.LoadOrCreateAsync(writeStore, eventId, cancellationToken);
        guard.SetCancelled();
    }
}
