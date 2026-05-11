using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.GetActiveReconfirmTriggerSpecs;

internal sealed class GetActiveReconfirmTriggerSpecsHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetActiveReconfirmTriggerSpecsQuery, IReadOnlyList<ReconfirmTriggerSpecDto>>
{
    public async ValueTask<IReadOnlyList<ReconfirmTriggerSpecDto>> HandleAsync(
        GetActiveReconfirmTriggerSpecsQuery query,
        CancellationToken cancellationToken)
    {
        return await writeStore.TicketedEvents
            .AsNoTracking()
            .Where(e => e.Status == EventLifecycleStatus.Active && e.ReconfirmPolicy != null)
            .Select(e => new ReconfirmTriggerSpecDto(
                e.TeamId.Value,
                e.Id.Value,
                e.TimeZone.Value,
                e.ReconfirmPolicy!.OpensAt,
                e.ReconfirmPolicy.ClosesAt,
                (int)e.ReconfirmPolicy.Cadence.TotalDays))
            .ToListAsync(cancellationToken);
    }
}
