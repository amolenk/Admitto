using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.EventLifecycleSync.HandleEventCancelled.EventHandlers;

internal sealed class TicketedEventCancelledModuleEventHandler(IMediator mediator)
    : IModuleEventHandler<TicketedEventCancelledModuleEvent>
{
    public ValueTask HandleAsync(TicketedEventCancelledModuleEvent moduleEvent, CancellationToken cancellationToken) =>
        mediator.SendAsync(
            new HandleEventCancelledCommand(moduleEvent.TicketedEventId)
            {
                CommandId = DeterministicCommandId<HandleEventCancelledCommand>.Create(moduleEvent.EventId)
            },
            cancellationToken);
}
