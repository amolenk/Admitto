using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.CancelTicketType;

internal sealed class CancelTicketTypeHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<CancelTicketTypeCommand>
{
    public async ValueTask HandleAsync(
        CancelTicketTypeCommand command,
        CancellationToken cancellationToken)
    {
        var policy = await writeStore.EventRegistrationPolicies
            .FirstOrDefaultAsync(p => p.Id == command.EventId, cancellationToken);

        if (policy is null)
            throw new BusinessRuleViolationException(EventRegistrationPolicy.Errors.EventNotFound);

        if (!policy.IsEventActive)
            throw new BusinessRuleViolationException(Errors.EventNotActive);

        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(tc => tc.Id == command.EventId, cancellationToken);

        if (catalog is null)
        {
            throw new BusinessRuleViolationException(
                NotFoundError.Create<TicketCatalog>(command.EventId.Value));
        }

        catalog.CancelTicketType(command.Slug);
    }

    internal static class Errors
    {
        public static readonly Error EventNotActive = new(
            "ticket_type.event_not_active",
            "Ticket types cannot be cancelled for cancelled or archived events.",
            Type: ErrorType.Validation);
    }
}
