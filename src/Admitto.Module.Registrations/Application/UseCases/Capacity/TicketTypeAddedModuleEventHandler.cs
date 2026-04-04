using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Capacity;

internal sealed class TicketTypeAddedModuleEventHandler(IRegistrationsWriteStore store)
    : IModuleEventHandler<TicketTypeAddedModuleEvent>
{
    public async ValueTask HandleAsync(TicketTypeAddedModuleEvent moduleEvent, CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(moduleEvent.TicketedEventId);

        var capacity = await store.EventCapacities
            .FirstOrDefaultAsync(ec => ec.Id == eventId, cancellationToken);

        if (capacity is null)
        {
            capacity = EventCapacity.Create(eventId);
            store.EventCapacities.Add(capacity);
        }

        capacity.SetTicketCapacity(moduleEvent.Slug, moduleEvent.Capacity);
    }
}
