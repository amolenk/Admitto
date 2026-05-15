using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

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

        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(tc => tc.Id == eventId, cancellationToken);

        if (catalog is null)
        {
            throw new BusinessRuleViolationException(
                NotFoundError.Create<TicketCatalog>(eventId.Value));
        }

        var timeSlots = command.TimeSlots
            .Select(s => TimeSlot.From(s))
            .ToArray();

        catalog.AddTicketType(id, name, timeSlots, command.MaxCapacity, command.SelfServiceEnabled);

        return id.Value;
    }
}

