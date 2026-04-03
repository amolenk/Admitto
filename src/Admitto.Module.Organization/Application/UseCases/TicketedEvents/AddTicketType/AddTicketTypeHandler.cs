using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.AddTicketType;

internal sealed class AddTicketTypeHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<AddTicketTypeCommand>
{
    public async ValueTask HandleAsync(AddTicketTypeCommand command, CancellationToken cancellationToken)
    {
        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            TicketedEventId.From(command.EventId),
            command.ExpectedVersion,
            cancellationToken);

        var timeSlots = command.TimeSlots
            .Select(s => new TimeSlot(Slug.From(s)))
            .ToArray();

        var capacity = command.Capacity.HasValue
            ? Capacity.From(command.Capacity.Value)
            : (Capacity?)null;

        ticketedEvent.AddTicketType(
            Slug.From(command.Slug),
            DisplayName.From(command.Name),
            command.IsSelfService,
            command.IsSelfServiceAvailable,
            timeSlots,
            capacity);
    }
}
