using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.UpdateTicketType;

internal sealed class UpdateTicketTypeHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<UpdateTicketTypeCommand>
{
    public async ValueTask HandleAsync(
        UpdateTicketTypeCommand command,
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
            throw new BusinessRuleViolationException(
                NotFoundError.Create<TicketCatalog>(command.EventId.Value));
        }

        catalog.UpdateTicketType(command.Slug, command.Name, command.MaxCapacity);
    }

    internal static class Errors
    {
        public static readonly Error EventNotActive = new(
            "ticket_type.event_not_active",
            "Ticket types cannot be updated for cancelled or archived events.",
            Type: ErrorType.Validation);
    }
}
