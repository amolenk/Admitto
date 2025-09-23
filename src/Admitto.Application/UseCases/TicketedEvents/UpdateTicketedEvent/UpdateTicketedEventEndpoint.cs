using Amolenk.Admitto.Application.Common;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.UpdateTicketedEvent;

/// <summary>
/// Updates details of a ticketed event.
/// </summary>
public static class UpdateTicketedEventEndpoint
{
    public static RouteGroupBuilder MapUpdateTicketedEvent(this RouteGroupBuilder group)
    {
        group
            .MapPatch("/{eventSlug}", UpdateTicketedEvent)
            .WithName(nameof(UpdateTicketedEvent))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok> UpdateTicketedEvent(
        string teamSlug,
        string eventSlug,
        UpdateTicketedEventRequest request,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        var ticketedEvent = await context.TicketedEvents
            .FirstOrDefaultAsync(te => te.Id == eventId, cancellationToken);
        if (ticketedEvent is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }

        ticketedEvent.UpdateDetails(
            request.Name,
            request.Website,
            request.BaseUrl,
            request.StartsAt?.ToUniversalTime(),
            request.EndsAt?.ToUniversalTime());
        
        return TypedResults.Ok();
    }
}