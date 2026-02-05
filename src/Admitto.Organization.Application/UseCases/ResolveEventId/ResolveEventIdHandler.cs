using Amolenk.Admitto.Organization.Application.Persistence;
using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Organization.Application.UseCases.ResolveEventId;

internal class ResolveEventIdHandler(IOrganizationWriteStore writeStore)
    : IQueryHandler<ResolveEventIdQuery, Guid>
{
    public async ValueTask<Guid> HandleAsync(
        ResolveEventIdQuery query,
        CancellationToken cancellationToken)
    {
        var ticketedEventId = await writeStore.TicketedEvents
            .AsNoTracking()
            .Where(e => e.TeamId == query.TeamId.Value && e.Slug == query.EventSlug)
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return ticketedEventId ??
               throw new BusinessRuleViolationException(Errors.TicketedEventNotFound(query.TeamId, query.EventSlug));
    }

    private static class Errors
    {
        public static Error TicketedEventNotFound(TeamId teamId, TicketedEventSlug eventSlug) =>
            new(
                "event_not_found",
                "Event could not be found.",
                Details: new Dictionary<string, object?>
                {
                    ["teamId"] = teamId,
                    ["eventSlug"] = eventSlug
                });
    }
}