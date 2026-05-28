using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.UpdateTicketedEventTimeZone;

internal sealed class UpdateTicketedEventTimeZoneHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<UpdateTicketedEventTimeZoneCommand>
{
    public async ValueTask HandleAsync(
        UpdateTicketedEventTimeZoneCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        TeamId teamId = TeamId.From(command.TeamId);
        TimeZoneId timeZone = TimeZoneId.From(command.TimeZone);

        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            e => e.Id == eventId && e.TeamId == teamId,
            command.ExpectedVersion,
            cancellationToken);

        ticketedEvent.ChangeTimeZone(timeZone);
    }
}
