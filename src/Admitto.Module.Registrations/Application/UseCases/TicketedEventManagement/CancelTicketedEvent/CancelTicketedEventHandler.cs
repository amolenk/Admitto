using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.CancelTicketedEvent;

internal sealed class CancelTicketedEventHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<CancelTicketedEventCommand>
{
    public async ValueTask HandleAsync(
        CancelTicketedEventCommand command,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(command.EventId, cancellationToken);

        ticketedEvent.Cancel();
    }
}
