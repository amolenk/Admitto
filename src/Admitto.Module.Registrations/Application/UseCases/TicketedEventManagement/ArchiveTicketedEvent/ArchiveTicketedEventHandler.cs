using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ArchiveTicketedEvent;

internal sealed class ArchiveTicketedEventHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<ArchiveTicketedEventCommand>
{
    public async ValueTask HandleAsync(
        ArchiveTicketedEventCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);

        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(eventId, cancellationToken);

        ticketedEvent.Archive();
    }
}
