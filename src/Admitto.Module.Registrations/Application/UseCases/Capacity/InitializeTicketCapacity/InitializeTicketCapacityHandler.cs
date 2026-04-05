using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Capacity.InitializeTicketCapacity;

internal sealed class InitializeTicketCapacityHandler(IRegistrationsWriteStore store)
    : ICommandHandler<InitializeTicketCapacityCommand>
{
    public async ValueTask HandleAsync(InitializeTicketCapacityCommand command, CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.TicketedEventId);

        var capacity = await store.EventCapacities
            .FirstOrDefaultAsync(ec => ec.Id == eventId, cancellationToken);

        if (capacity is null)
        {
            capacity = EventCapacity.Create(eventId);
            store.EventCapacities.Add(capacity);
        }

        capacity.SetTicketCapacity(command.Slug, command.Capacity);
    }
}
