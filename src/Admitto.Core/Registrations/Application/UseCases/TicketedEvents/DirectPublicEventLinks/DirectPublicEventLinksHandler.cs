using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.DirectPublicEventLinks;

internal sealed class DirectPublicEventLinksHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<DirectPublicEventLinksQuery, DirectPublicEventLinkDto?>
{
    public async ValueTask<DirectPublicEventLinkDto?> HandleAsync(
        DirectPublicEventLinksQuery query,
        CancellationToken cancellationToken)
    {
        var slug = Slug.From(query.EventSlug);

        var urls = await writeStore.TicketedEvents
            .AsNoTracking()
            .Where(e => e.PublicSlug == slug)
            .Select(e => new
            {
                WebsiteUrl = e.WebsiteUrl.Value,
                BaseUrl = e.BaseUrl.Value
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (urls is null)
        {
            return null;
        }

        if (query.ActionPath is null)
        {
            return new DirectPublicEventLinkDto(urls.WebsiteUrl);
        }

        var segments = query.RegistrationId is null
            ? [query.ActionPath]
            : new[] { query.ActionPath, query.RegistrationId.Value.ToString() };

        return new DirectPublicEventLinkDto(BuildRelativeUrl(urls.BaseUrl, segments));
    }

    private string BuildRelativeUrl(string baseUrl, IReadOnlyCollection<string> pathSegments)
    {
        var builder = new UriBuilder(baseUrl);
        var basePath = builder.Path.TrimEnd('/');
        var suffix = string.Join('/', pathSegments.Select(Uri.EscapeDataString));

        builder.Path = string.IsNullOrEmpty(basePath)
            ? $"/{suffix}"
            : $"{basePath}/{suffix}";

        return builder.Uri.ToString();
    }
}
