using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.CancelTicketType;

internal sealed class CancelTicketTypeHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<CancelTicketTypeCommand>
{
    public async ValueTask HandleAsync(
        CancelTicketTypeCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        Slug slug = Slug.From(command.Slug);

        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(tc => tc.Id == eventId, cancellationToken);

        if (catalog is null)
        {
            throw new BusinessRuleViolationException(
                NotFoundError.Create<TicketCatalog>(eventId.Value));
        }

        catalog.CancelTicketType(slug);
    }
}

