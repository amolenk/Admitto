using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.AddTicketType;

internal sealed class AddTicketTypeHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<AddTicketTypeCommand, Guid>
{
    public async ValueTask<Guid> HandleAsync(
        AddTicketTypeCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        TicketTypeId id = TicketTypeId.New();
        TicketTypeName name = TicketTypeName.From(command.Name);

        var catalog = await writeStore.TicketCatalogs.GetAsync(
                 eventId,
                 cancellationToken);

        var timeSlots = command.TimeSlots
            .Select(TimeSlot.From)
            .ToArray();

        catalog.AddTicketType(id, name, timeSlots, command.MaxCapacity, command.SelfServiceEnabled);

        return id.Value;
    }
}

