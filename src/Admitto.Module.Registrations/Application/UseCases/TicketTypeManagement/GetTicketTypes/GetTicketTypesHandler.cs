using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.GetTicketTypes;

internal sealed class GetTicketTypesHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetTicketTypesQuery, IReadOnlyList<TicketTypeDto>>
{
    public async ValueTask<IReadOnlyList<TicketTypeDto>> HandleAsync(
        GetTicketTypesQuery query,
        CancellationToken cancellationToken)
    {
        var catalog = await writeStore.TicketCatalogs
            .AsNoTracking()
            .FirstOrDefaultAsync(tc => tc.Id == query.EventId, cancellationToken);

        if (catalog is null)
        {
            return [];
        }

        return catalog.TicketTypes
            .Select(tt => new TicketTypeDto(
                tt.Id,
                tt.Name.Value,
                tt.TimeSlotSlugs,
                tt.MaxCapacity,
                tt.UsedCapacity,
                tt.IsCancelled))
            .ToList();
    }
}
