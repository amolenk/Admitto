using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.MaterializeTicketedEvent;

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
            command.CreationRequestId,
            ticketedEventId,
            teamId,
            DisplayName.From(command.Name),
            AbsoluteUrl.From(command.WebsiteUrl),
            AbsoluteUrl.From(command.BaseUrl),
            command.StartsAt,
            command.EndsAt,
            timeZone);

        var catalog = TicketCatalog.Create(ticketedEventId);

        writeStore.TicketedEvents.Add(ticketedEvent);
        writeStore.TicketCatalogs.Add(catalog);

        return ValueTask.CompletedTask;
    }
}
