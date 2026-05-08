using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.UpdateTicketType;

internal sealed class UpdateTicketTypeHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<UpdateTicketTypeCommand>
{
    public async ValueTask HandleAsync(
        UpdateTicketTypeCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        Slug slug = Slug.From(command.Slug);
        DisplayName? name = command.Name is not null ? DisplayName.From(command.Name) : null;

        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(tc => tc.Id == eventId, cancellationToken);

        if (catalog is null)
        {
            throw new BusinessRuleViolationException(
                NotFoundError.Create<TicketCatalog>(eventId.Value));
        }

        catalog.UpdateTicketType(slug, name, command.MaxCapacity, command.SelfServiceEnabled);
    }
}

