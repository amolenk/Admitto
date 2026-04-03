using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.UpdateTicketedEvent;

internal sealed class UpdateTicketedEventHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<UpdateTicketedEventCommand>
{
    public async ValueTask HandleAsync(UpdateTicketedEventCommand command, CancellationToken cancellationToken)
    {
        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            TicketedEventId.From(command.EventId),
            command.ExpectedVersion,
            cancellationToken);

        var eventWindow = command.StartsAt.HasValue || command.EndsAt.HasValue
            ? new TimeWindow(
                command.StartsAt ?? ticketedEvent.EventWindow.Start,
                command.EndsAt ?? ticketedEvent.EventWindow.End)
            : null;

        ticketedEvent.Update(
            command.Name is not null ? DisplayName.From(command.Name) : null,
            command.WebsiteUrl is not null ? AbsoluteUrl.From(command.WebsiteUrl) : null,
            command.BaseUrl is not null ? AbsoluteUrl.From(command.BaseUrl) : null,
            eventWindow);
    }
}
