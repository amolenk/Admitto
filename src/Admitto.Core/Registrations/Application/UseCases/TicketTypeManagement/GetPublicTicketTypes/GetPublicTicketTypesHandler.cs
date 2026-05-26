using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.GetPublicTicketTypes;

internal sealed class GetPublicTicketTypesHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetPublicTicketTypesQuery, IReadOnlyList<PublicTicketTypeDto>>
{
    public async ValueTask<IReadOnlyList<PublicTicketTypeDto>> HandleAsync(
        GetPublicTicketTypesQuery query,
        CancellationToken cancellationToken)
    {
        var catalog = await writeStore.TicketCatalogs.GetUntrackedAsync(
             tc => tc.Id == query.EventId,
             cancellationToken);

        return catalog.TicketTypes
            .Where(tt => tt.SelfServiceEnabled)
            .Select(tt => new PublicTicketTypeDto(
                tt.Id.Value,
                tt.Name.Value,
                tt.TimeSlots.Select(ts => ts.Value).ToArray(),
                tt.MaxCapacity,
                tt.UsedCapacity,
                tt.WaitlistEnabled,
                tt.WaitlistMode))
            .ToList();
    }
}
