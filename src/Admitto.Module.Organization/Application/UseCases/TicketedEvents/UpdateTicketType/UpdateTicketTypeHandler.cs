using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.UpdateTicketType;

internal sealed class UpdateTicketTypeHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<UpdateTicketTypeCommand>
{
    public async ValueTask HandleAsync(UpdateTicketTypeCommand command, CancellationToken cancellationToken)
    {
        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            TicketedEventId.From(command.EventId),
            command.ExpectedVersion,
            cancellationToken);

        ticketedEvent.UpdateTicketType(
            Slug.From(command.TicketTypeSlug),
            command.Name is not null ? DisplayName.From(command.Name) : null,
            command.Capacity.HasValue ? Capacity.From(command.Capacity.Value) : (Capacity?)null,
            command.IsSelfServiceAvailable);
    }
}
