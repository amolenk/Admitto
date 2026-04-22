using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventDetails;

internal sealed class UpdateTicketedEventDetailsHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<UpdateTicketedEventDetailsCommand>
{
    public async ValueTask HandleAsync(
        UpdateTicketedEventDetailsCommand command,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            command.EventId,
            command.ExpectedVersion,
            cancellationToken);

        ticketedEvent.UpdateDetails(command.Name, command.WebsiteUrl, command.BaseUrl, command.StartsAt, command.EndsAt);
    }
}
