using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.GetTicketTypes;

internal sealed class GetTicketTypesHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetTicketTypesQuery, IReadOnlyList<TicketTypeDto>>
{
    public async ValueTask<IReadOnlyList<TicketTypeDto>> HandleAsync(
        GetTicketTypesQuery query,
        CancellationToken cancellationToken)
    {
        var catalog = await writeStore.TicketCatalogs
            .AsNoTracking()
            .FirstOrDefaultAsync(tc => tc.Id == query.EventId && tc.TeamId == query.TeamId, cancellationToken);

        if (catalog is null)
        {
            return [];
        }

        return catalog.TicketTypes
            .Select(tt => new TicketTypeDto(
                tt.Id.Value,
                tt.Name.Value,
                tt.TimeSlots.Select(ts => ts.Value).ToArray(),
                tt.MaxCapacity,
                tt.UsedCapacity,
                tt.SelfServiceEnabled,
                tt.WaitlistEnabled,
                tt.WaitlistMode,
                tt.ClaimWindowHours,
                tt.MaxReconfirmAttempts))
            .ToList();
    }
}
