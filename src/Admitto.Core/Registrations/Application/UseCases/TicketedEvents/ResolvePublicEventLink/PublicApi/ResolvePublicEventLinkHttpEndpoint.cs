using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ResolvePublicEventLink.PublicApi;

public static class ResolvePublicEventLinkHttpEndpoint
{
    public static RouteGroupBuilder MapResolvePublicEventLink(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{publicSlug}", ResolvePublicEventLink)
            .WithName(nameof(ResolvePublicEventLink))
            .Produces(StatusCodes.Status302Found)
            .Produces(StatusCodes.Status404NotFound);

        return group;
    }

    private static async ValueTask<Results<RedirectHttpResult, NotFound>> ResolvePublicEventLink(
        string publicSlug,
        IQueryHandler<ResolvePublicEventLinkQuery, PublicEventLinkDto?> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ResolvePublicEventLinkQuery(publicSlug), ct);

        return result is null
            ? TypedResults.NotFound()
            : TypedResults.Redirect(result.WebsiteUrl, permanent: false);
    }
}
