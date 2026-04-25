using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEvents.GetActiveReconfirmTriggerSpecs;

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
