using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.CancelTicketType;

internal sealed class CancelTicketTypeHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<CancelTicketTypeCommand>
{
    public async ValueTask HandleAsync(CancelTicketTypeCommand command, CancellationToken cancellationToken)
    {
        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            TicketedEventId.From(command.EventId),
            command.ExpectedVersion,
            cancellationToken);

        ticketedEvent.CancelTicketType(Slug.From(command.TicketTypeSlug));
    }
}
