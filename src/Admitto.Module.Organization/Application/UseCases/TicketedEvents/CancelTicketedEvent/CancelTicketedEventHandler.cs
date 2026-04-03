using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.CancelTicketedEvent;

internal sealed class CancelTicketedEventHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<CancelTicketedEventCommand>
{
    public async ValueTask HandleAsync(CancelTicketedEventCommand command, CancellationToken cancellationToken)
    {
        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            TicketedEventId.From(command.EventId),
            command.ExpectedVersion,
            cancellationToken);

        ticketedEvent.Cancel();
    }
}
