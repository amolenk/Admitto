using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.AddTicketType;

internal sealed class AddTicketTypeHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<AddTicketTypeCommand>
{
    public async ValueTask HandleAsync(
        AddTicketTypeCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        Slug slug = Slug.From(command.Slug);
        DisplayName name = DisplayName.From(command.Name);

        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(tc => tc.Id == eventId, cancellationToken);

        if (catalog is null)
        {
            throw new BusinessRuleViolationException(
                NotFoundError.Create<TicketCatalog>(eventId.Value));
        }

        var timeSlots = command.TimeSlots
            .Select(s => new TimeSlot(Slug.From(s)))
            .ToArray();

        catalog.AddTicketType(slug, name, timeSlots, command.MaxCapacity, command.SelfServiceEnabled);
    }
}

