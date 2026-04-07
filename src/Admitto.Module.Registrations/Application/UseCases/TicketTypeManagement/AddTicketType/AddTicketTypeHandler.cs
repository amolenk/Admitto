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
        var policy = await writeStore.EventRegistrationPolicies
            .FirstOrDefaultAsync(p => p.Id == command.EventId, cancellationToken);

        if (policy is null || !policy.IsEventActive)
        {
            throw new BusinessRuleViolationException(Errors.EventNotActive);
        }

        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(tc => tc.Id == command.EventId, cancellationToken);

        if (catalog is null)
        {
            catalog = TicketCatalog.Create(command.EventId);
            await writeStore.TicketCatalogs.AddAsync(catalog, cancellationToken);
        }

        var timeSlots = command.TimeSlots
            .Select(s => new TimeSlot(Slug.From(s)))
            .ToArray();

        catalog.AddTicketType(command.Slug, command.Name, timeSlots, command.MaxCapacity);
    }

    internal static class Errors
    {
        public static readonly Error EventNotActive = new(
            "ticket_type.event_not_active",
            "Ticket types cannot be added for cancelled or archived events.",
            Type: ErrorType.Validation);
    }
}
