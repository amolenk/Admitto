using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.DirectPublicEventLinks.PublicApi;

public static class DirectPublicEventLinksHttpEndpoint
{
    public static RouteGroupBuilder MapDirectPublicEventLinks(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{eventSlug}", RedirectToEventWebsite)
            .WithName(nameof(RedirectToEventWebsite));

        group
            .MapGet("/{eventSlug}/register", RedirectToRegister)
            .WithName(nameof(RedirectToRegister));

        group
            .MapGet("/{eventSlug}/cancel/{registrationId:guid}", RedirectToCancel)
            .WithName(nameof(RedirectToCancel));

        group
            .MapGet("/{eventSlug}/edit/{registrationId:guid}", RedirectToEdit)
            .WithName(nameof(RedirectToEdit));

        return group;
    }

    private static ValueTask<Results<RedirectHttpResult, NotFound>> RedirectToEventWebsite(
        string eventSlug,
        IQueryHandler<DirectPublicEventLinksQuery, DirectPublicEventLinkDto?> handler,
        CancellationToken ct) => RedirectToLink(eventSlug, null, null, handler, ct);

    private static ValueTask<Results<RedirectHttpResult, NotFound>> RedirectToRegister(
        string eventSlug,
        IQueryHandler<DirectPublicEventLinksQuery, DirectPublicEventLinkDto?> handler,
        CancellationToken ct) => RedirectToLink(eventSlug, "register", null, handler, ct);

    private static ValueTask<Results<RedirectHttpResult, NotFound>> RedirectToCancel(
        string eventSlug,
        Guid registrationId,
        IQueryHandler<DirectPublicEventLinksQuery, DirectPublicEventLinkDto?> handler,
        CancellationToken ct) => RedirectToLink(eventSlug, "cancel", registrationId, handler, ct);

    private static ValueTask<Results<RedirectHttpResult, NotFound>> RedirectToEdit(
        string eventSlug,
        Guid registrationId,
        IQueryHandler<DirectPublicEventLinksQuery, DirectPublicEventLinkDto?> handler,
        CancellationToken ct) => RedirectToLink(eventSlug, "edit", registrationId, handler, ct);

    private static async ValueTask<Results<RedirectHttpResult, NotFound>> RedirectToLink(
        string eventSlug,
        string? actionPath,
        Guid? registrationId,
        IQueryHandler<DirectPublicEventLinksQuery, DirectPublicEventLinkDto?> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(
            new DirectPublicEventLinksQuery(eventSlug, actionPath, registrationId), ct);

        return result is null
            ? TypedResults.NotFound()
            : TypedResults.Redirect(result.RedirectUrl, permanent: false);
    }
}
