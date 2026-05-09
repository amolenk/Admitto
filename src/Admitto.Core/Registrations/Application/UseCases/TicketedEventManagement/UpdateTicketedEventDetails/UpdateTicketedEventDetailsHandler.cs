using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventDetails;

internal sealed class UpdateTicketedEventDetailsHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<UpdateTicketedEventDetailsCommand>
{
    public async ValueTask HandleAsync(
        UpdateTicketedEventDetailsCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        DisplayName name = DisplayName.From(command.Name);
        AbsoluteUrl websiteUrl = AbsoluteUrl.From(command.WebsiteUrl);
        AbsoluteUrl baseUrl = AbsoluteUrl.From(command.BaseUrl);

        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            eventId,
            command.ExpectedVersion,
            cancellationToken);

        ticketedEvent.UpdateDetails(name, websiteUrl, baseUrl, command.StartsAt, command.EndsAt);
    }
}
