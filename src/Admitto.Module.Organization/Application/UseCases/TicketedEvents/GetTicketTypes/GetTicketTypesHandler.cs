using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.GetTicketTypes;

internal class GetTicketTypesHandler(IOrganizationWriteStore writeStore)
    : IQueryHandler<GetTicketTypesQuery, TicketTypeDto[]>
{
    public async ValueTask<TicketTypeDto[]> HandleAsync(
        GetTicketTypesQuery query,
        CancellationToken cancellationToken)
    {
        var ticketedEventId = TicketedEventId.From(query.TicketedEventId);

        var ticketedEvent = await writeStore.TicketedEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == ticketedEventId, cancellationToken);

        if (ticketedEvent is null) return [];

        return ticketedEvent.TicketTypes
            .Select(tt => new TicketTypeDto
            {
                Slug = tt.Slug.Value,
                Name = tt.Name.Value,
                TimeSlots = tt.TimeSlots.Select(ts => ts.Slug.Value).ToList(),
                Capacity = tt.Capacity == null ? (int?)null : tt.Capacity.Value.Value,
                IsCancelled = tt.IsCancelled
            })
            .ToArray();
    }
}