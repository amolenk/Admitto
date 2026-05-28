using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.AddTicketType;

internal sealed class AddTicketTypeHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<AddTicketTypeCommand, Guid>
{
    public async ValueTask<Guid> HandleAsync(
        AddTicketTypeCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        TeamId teamId = TeamId.From(command.TeamId);
        TicketTypeId id = TicketTypeId.New();
        TicketTypeName name = TicketTypeName.From(command.Name);

        var catalog = await writeStore.TicketCatalogs.GetAsync(
                 tc => tc.Id == eventId && tc.TeamId == teamId,
                 cancellationToken);

        var timeSlots = command.TimeSlots
            .Select(TimeSlot.From)
            .ToArray();

        catalog.AddTicketType(id, name, timeSlots, command.MaxCapacity, command.SelfServiceEnabled,
            command.WaitlistEnabled, command.ClaimWindowHours, command.MaxReconfirmAttempts);

        return id.Value;
    }
}

