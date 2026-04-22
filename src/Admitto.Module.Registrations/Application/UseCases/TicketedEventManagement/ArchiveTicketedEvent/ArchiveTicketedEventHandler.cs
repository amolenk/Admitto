using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ArchiveTicketedEvent;

internal sealed class ArchiveTicketedEventHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<ArchiveTicketedEventCommand>
{
    public async ValueTask HandleAsync(
        ArchiveTicketedEventCommand command,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(command.EventId, cancellationToken);

        ticketedEvent.Archive();
    }
}
