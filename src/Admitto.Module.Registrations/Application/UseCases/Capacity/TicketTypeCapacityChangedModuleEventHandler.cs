using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Capacity;

internal sealed class TicketTypeCapacityChangedModuleEventHandler(IRegistrationsWriteStore store)
    : IModuleEventHandler<TicketTypeCapacityChangedModuleEvent>
{
    public async ValueTask HandleAsync(
        TicketTypeCapacityChangedModuleEvent moduleEvent,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(moduleEvent.TicketedEventId);

        var capacity = await store.EventCapacities
            .FirstOrDefaultAsync(ec => ec.Id == eventId, cancellationToken);

        if (capacity is null) return;

        capacity.SetTicketCapacity(moduleEvent.Slug, moduleEvent.Capacity);
    }
}
