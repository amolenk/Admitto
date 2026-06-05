using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.MaterializeTicketedEvent;

internal sealed class MaterializeTicketedEventHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<MaterializeTicketedEventCommand>
{
    public ValueTask HandleAsync(
        MaterializeTicketedEventCommand command,
        CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(command.TeamId);
        var ticketedEventId = TicketedEventId.New();
        var timeZone = TimeZoneId.From(command.TimeZone);

        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(command.CreationRequestId),
            ticketedEventId,
            teamId,
            EventName.From(command.Name),
            AbsoluteUrl.From(command.WebsiteUrl),
            AbsoluteUrl.From(command.BaseUrl),
            command.StartsAt,
            command.EndsAt,
            timeZone);

        var catalog = TicketCatalog.Create(ticketedEventId, teamId);

        writeStore.TicketedEvents.Add(ticketedEvent);
        writeStore.TicketCatalogs.Add(catalog);

        return ValueTask.CompletedTask;
    }
}
