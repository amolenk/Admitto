using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.GetPublicTicketTypes;

internal sealed class GetPublicTicketTypesHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetPublicTicketTypesQuery, IReadOnlyList<PublicTicketTypeDto>>
{
    public async ValueTask<IReadOnlyList<PublicTicketTypeDto>> HandleAsync(
        GetPublicTicketTypesQuery query,
        CancellationToken cancellationToken)
    {
        var catalog = await writeStore.TicketCatalogs
            .AsNoTracking()
            .FirstOrDefaultAsync(tc => tc.Id == query.EventId, cancellationToken);

        if (catalog is null)
            throw new BusinessRuleViolationException(
                NotFoundError.Create<TicketCatalog>(query.EventId.Value));

        return catalog.TicketTypes
            .Where(tt => !tt.IsCancelled && tt.SelfServiceEnabled)
            .Select(tt => new PublicTicketTypeDto(
                tt.Id,
                tt.Name.Value,
                tt.TimeSlotSlugs,
                tt.MaxCapacity,
                tt.UsedCapacity))
            .ToList();
    }
}
