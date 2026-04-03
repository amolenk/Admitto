using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.ArchiveTicketedEvent;

internal sealed class ArchiveTicketedEventHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<ArchiveTicketedEventCommand>
{
    public async ValueTask HandleAsync(ArchiveTicketedEventCommand command, CancellationToken cancellationToken)
    {
        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            TicketedEventId.From(command.EventId),
            command.ExpectedVersion,
            cancellationToken);

        ticketedEvent.Archive();
    }
}
