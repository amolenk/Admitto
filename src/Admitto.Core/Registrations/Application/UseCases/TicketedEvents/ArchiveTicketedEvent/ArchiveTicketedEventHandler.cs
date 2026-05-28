using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ArchiveTicketedEvent;

internal sealed class ArchiveTicketedEventHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<ArchiveTicketedEventCommand>
{
    public async ValueTask HandleAsync(
        ArchiveTicketedEventCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        TeamId teamId = TeamId.From(command.TeamId);

        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            e => e.Id == eventId && e.TeamId == teamId,
            cancellationToken);

        ticketedEvent.Archive();
    }
}
