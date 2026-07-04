using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ResolvePartnerTicketedEvent.PartnerApi;

internal sealed class PartnerTicketedEventResolver(IRegistrationsWriteStore writeStore)
{
    public async ValueTask<TicketedEventId> ResolveAsync(
        TeamId teamId,
        string eventSlug,
        CancellationToken cancellationToken)
    {
        var slug = Slug.From(eventSlug);

        var result = await writeStore.TicketedEvents
            .AsNoTracking()
            .Where(e => e.TeamId == teamId && e.PublicSlug == slug)
            .Select(e => new { e.Id })
            .FirstOrDefaultAsync(cancellationToken);

        return result?.Id ?? throw new BusinessRuleViolationException(NotFoundError.Create<TicketedEvent>());
    }
}
