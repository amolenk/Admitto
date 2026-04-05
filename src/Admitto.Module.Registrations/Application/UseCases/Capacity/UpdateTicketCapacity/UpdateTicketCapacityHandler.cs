using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Capacity.UpdateTicketCapacity;

internal sealed class UpdateTicketCapacityHandler(IRegistrationsWriteStore store)
    : ICommandHandler<UpdateTicketCapacityCommand>
{
    public async ValueTask HandleAsync(UpdateTicketCapacityCommand command, CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.TicketedEventId);

        var capacity = await store.EventCapacities
            .FirstOrDefaultAsync(ec => ec.Id == eventId, cancellationToken);

        if (capacity is null) return;

        capacity.SetTicketCapacity(command.Slug, command.Capacity);
    }
}
