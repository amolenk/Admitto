using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Capacity.UpdateTicketCapacity.EventHandlers;

internal sealed class TicketTypeCapacityChangedModuleEventHandler(IMediator mediator)
    : IModuleEventHandler<TicketTypeCapacityChangedModuleEvent>
{
    public ValueTask HandleAsync(
        TicketTypeCapacityChangedModuleEvent moduleEvent,
        CancellationToken cancellationToken) =>
        mediator.SendAsync(
            new UpdateTicketCapacityCommand(
                moduleEvent.TicketedEventId, moduleEvent.Slug, moduleEvent.Capacity)
            {
                CommandId = DeterministicCommandId<UpdateTicketCapacityCommand>.Create(moduleEvent.EventId)
            },
            cancellationToken);
}
