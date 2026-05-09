using Amolenk.Admitto.Core.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.TicketedEventManagement.ArchiveTicketedEvent;

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
