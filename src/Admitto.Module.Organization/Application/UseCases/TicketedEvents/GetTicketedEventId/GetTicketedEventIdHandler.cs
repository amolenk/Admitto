using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.GetTicketedEventId;

internal class GetTicketedEventIdHandler(IOrganizationWriteStore writeStore)
    : IQueryHandler<GetTicketedEventIdQuery, Guid>
{
    public async ValueTask<Guid> HandleAsync(
        GetTicketedEventIdQuery query,
        CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(query.TeamId);
        var eventSlug = Slug.From(query.EventSlug);
        
        var ticketedEventId = await writeStore.TicketedEvents
            .AsNoTracking()
            .Where(e => e.TeamId == teamId && e.Slug == eventSlug)
            .Select(e => (Guid?)e.Id.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return ticketedEventId ??
               throw new BusinessRuleViolationException(Errors.TicketedEventNotFound(query.TeamId, query.EventSlug));
    }

    private static class Errors
    {
        public static Error TicketedEventNotFound(Guid teamId, string eventSlug) =>
            new(
                "event.not_found",
                "Event could not be found.",
                Details: new Dictionary<string, object?>
                {
                    ["teamId"] = teamId,
                    ["eventSlug"] = eventSlug
                });
    }
}