using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Capacity.InitializeTicketCapacity.EventHandlers;

internal sealed class TicketTypeAddedModuleEventHandler(IMediator mediator)
    : IModuleEventHandler<TicketTypeAddedModuleEvent>
{
    public ValueTask HandleAsync(TicketTypeAddedModuleEvent moduleEvent, CancellationToken cancellationToken) =>
        mediator.SendAsync(
            new InitializeTicketCapacityCommand(
                moduleEvent.TicketedEventId, moduleEvent.Slug, moduleEvent.Capacity)
            {
                CommandId = DeterministicCommandId<InitializeTicketCapacityCommand>.Create(moduleEvent.EventId)
            },
            cancellationToken);
}
