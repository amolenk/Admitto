using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ResolvePublicEventLink;

internal sealed class ResolvePublicEventLinkHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<ResolvePublicEventLinkQuery, PublicEventLinkDto?>
{
    public async ValueTask<PublicEventLinkDto?> HandleAsync(
        ResolvePublicEventLinkQuery query,
        CancellationToken cancellationToken)
    {
        var slug = Slug.From(query.PublicSlug);

        return await writeStore.TicketedEvents
            .AsNoTracking()
            .Where(e => e.PublicSlug == slug)
            .Select(e => new PublicEventLinkDto(e.PublicSlug.Value, e.WebsiteUrl.Value.ToString()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
