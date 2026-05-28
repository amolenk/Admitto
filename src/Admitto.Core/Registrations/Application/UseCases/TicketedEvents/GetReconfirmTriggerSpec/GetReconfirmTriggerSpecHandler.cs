using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetReconfirmTriggerSpec;

internal sealed class GetReconfirmTriggerSpecHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetReconfirmTriggerSpecQuery, ReconfirmTriggerSpecDto?>
{
    public async ValueTask<ReconfirmTriggerSpecDto?> HandleAsync(
        GetReconfirmTriggerSpecQuery query,
        CancellationToken cancellationToken)
    {
        var ticketedEventId = TicketedEventId.From(query.TicketedEventId);

        return await writeStore.TicketedEvents
            .AsNoTracking()
            .Where(e => e.Id == ticketedEventId
                        && e.Status == EventLifecycleStatus.Active
                        && e.ReconfirmPolicy != null)
            .Select(e => new ReconfirmTriggerSpecDto(
                e.TeamId.Value,
                e.Id.Value,
                e.TimeZone.Value,
                e.ReconfirmPolicy!.OpensAt,
                e.ReconfirmPolicy.ClosesAt,
                (int)e.ReconfirmPolicy.Cadence.TotalHours,
                (int)e.ReconfirmPolicy.MinEmailInterval.TotalHours))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
