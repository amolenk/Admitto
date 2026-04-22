using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventId;

internal class GetTicketedEventIdHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetTicketedEventIdQuery, Guid>
{
    public async ValueTask<Guid> HandleAsync(
        GetTicketedEventIdQuery query,
        CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(query.TeamId);
        var slug = Slug.From(query.EventSlug);

        var ticketedEventId = await writeStore.TicketedEvents
            .AsNoTracking()
            .Where(e => e.TeamId == teamId && e.Slug == slug)
            .Select(e => (Guid?)e.Id.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return ticketedEventId
            ?? throw new BusinessRuleViolationException(NotFoundError.Create<TicketedEvent>(query.EventSlug));
    }
}
