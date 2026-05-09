using Amolenk.Admitto.Core.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.TicketedEventManagement.CancelTicketedEvent;

internal sealed class CancelTicketedEventHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<CancelTicketedEventCommand>
{
    public async ValueTask HandleAsync(
        CancelTicketedEventCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);

        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(eventId, cancellationToken);

        ticketedEvent.Cancel();
    }
}
