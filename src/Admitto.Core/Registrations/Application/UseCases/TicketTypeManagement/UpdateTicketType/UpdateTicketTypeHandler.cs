using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.UpdateTicketType;

internal sealed class UpdateTicketTypeHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<UpdateTicketTypeCommand>
{
    public async ValueTask HandleAsync(
        UpdateTicketTypeCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        Slug slug = Slug.From(command.Slug);
        TicketTypeName? name = command.Name is not null ? TicketTypeName.From(command.Name) : null;

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

