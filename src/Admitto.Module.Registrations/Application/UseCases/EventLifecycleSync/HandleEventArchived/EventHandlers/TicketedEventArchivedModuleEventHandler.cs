using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.EventLifecycleSync.HandleEventArchived.EventHandlers;

internal sealed class TicketedEventArchivedModuleEventHandler(IMediator mediator)
    : IModuleEventHandler<TicketedEventArchivedModuleEvent>
{
    public ValueTask HandleAsync(TicketedEventArchivedModuleEvent moduleEvent, CancellationToken cancellationToken) =>
        mediator.SendAsync(
            new HandleEventArchivedCommand(moduleEvent.TicketedEventId)
            {
                CommandId = DeterministicCommandId<HandleEventArchivedCommand>.Create(moduleEvent.EventId)
            },
            cancellationToken);
}
