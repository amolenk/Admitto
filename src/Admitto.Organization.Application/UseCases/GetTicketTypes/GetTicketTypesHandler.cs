using Amolenk.Admitto.Organization.Application.Persistence;
using Amolenk.Admitto.Organization.Contracts;
using Amolenk.Admitto.Registrations.Application.Mapping;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Organization.Application.UseCases.GetTicketTypes;

internal class GetTicketTypesHandler(IOrganizationWriteStore writeStore)
    : IQueryHandler<GetTicketTypesQuery, TicketTypeDto[]>
{
    public async ValueTask<TicketTypeDto[]> HandleAsync(
        GetTicketTypesQuery query,
        CancellationToken cancellationToken)
    {
        var allEvents = await writeStore.TicketedEvents
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
        
        
        var ticketTypes = await writeStore.TicketedEvents
            .AsNoTracking()
            .Where(t => t.Id == query.TicketedEventId.Value)
            .SelectMany(t => t.TicketTypes.Select(tt => tt.ToDto()))
            .ToArrayAsync(cancellationToken);

        return ticketTypes;
    }
}