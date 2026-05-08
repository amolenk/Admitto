using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventTimeZone;

internal sealed class UpdateTicketedEventTimeZoneHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<UpdateTicketedEventTimeZoneCommand>
{
    public async ValueTask HandleAsync(
        UpdateTicketedEventTimeZoneCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        TimeZoneId timeZone = TimeZoneId.From(command.TimeZone);

        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            eventId,
            command.ExpectedVersion,
            cancellationToken);

        ticketedEvent.ChangeTimeZone(timeZone);
    }
}
