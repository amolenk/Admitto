using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEvents.GetReconfirmTriggerSpec;

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
                (int)e.ReconfirmPolicy.Cadence.TotalDays))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
