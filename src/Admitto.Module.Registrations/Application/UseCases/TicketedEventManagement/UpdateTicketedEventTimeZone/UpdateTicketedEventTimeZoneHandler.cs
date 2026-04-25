using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventTimeZone;

internal sealed class UpdateTicketedEventTimeZoneHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<UpdateTicketedEventTimeZoneCommand>
{
    public async ValueTask HandleAsync(
        UpdateTicketedEventTimeZoneCommand command,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            command.EventId,
            command.ExpectedVersion,
            cancellationToken);

        ticketedEvent.ChangeTimeZone(command.TimeZone);
    }
}
