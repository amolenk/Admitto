using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.GetPublicTicketTypes;

internal sealed class GetPublicTicketTypesHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetPublicTicketTypesQuery, IReadOnlyList<PublicTicketTypeDto>>
{
    public async ValueTask<IReadOnlyList<PublicTicketTypeDto>> HandleAsync(
        GetPublicTicketTypesQuery query,
        CancellationToken cancellationToken)
    {
        var catalog = await writeStore.TicketCatalogs.GetUntrackedAsync(
             tc => tc.Id == query.EventId && tc.TeamId == query.TeamId,
             cancellationToken);

        return catalog.TicketTypes
            .Where(tt => tt.SelfServiceEnabled)
            .Select(tt => new PublicTicketTypeDto(
                tt.Id.Value,
                tt.Name.Value,
                tt.TimeSlots.Select(ts => ts.Value).ToArray(),
                GetStatus(tt.WaitlistMode, tt.IsSoldOut)))
            .ToList();
    }

    private static PublicTicketStatus GetStatus(bool waitlistMode, bool isSoldOut) =>
        waitlistMode
            ? PublicTicketStatus.Waitlist
            : isSoldOut
                ? PublicTicketStatus.SoldOut
                : PublicTicketStatus.Available;
}
