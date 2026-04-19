using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.EventLifecycleSync.HandleEventCreated.EventHandlers;

internal sealed class TicketedEventCreatedModuleEventHandler(IMediator mediator)
    : IModuleEventHandler<TicketedEventCreatedModuleEvent>
{
    public ValueTask HandleAsync(TicketedEventCreatedModuleEvent moduleEvent, CancellationToken cancellationToken) =>
        mediator.SendAsync(
            new HandleEventCreatedCommand(moduleEvent.TicketedEventId)
            {
                CommandId = DeterministicCommandId<HandleEventCreatedCommand>.Create(moduleEvent.EventId)
            },
            cancellationToken);
}
