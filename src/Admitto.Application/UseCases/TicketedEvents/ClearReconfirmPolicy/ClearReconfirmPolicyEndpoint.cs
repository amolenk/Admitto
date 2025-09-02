using Amolenk.Admitto.Application.Common;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.ClearReconfirmPolicy;

/// <summary>
/// Clears the reconfirm policy on a ticketed event.
/// </summary>
public static class ClearReconfirmPolicyEndpoint
{
    public static RouteGroupBuilder MapClearReconfirmPolicy(this RouteGroupBuilder group)
    {
        group
            .MapDelete("/{eventSlug}/policies/reconfirm", ClearReconfirmPolicy)
            .WithName(nameof(ClearReconfirmPolicy))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok> ClearReconfirmPolicy(
        string teamSlug,
        string eventSlug,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        var ticketedEvent = await context.TicketedEvents.FindAsync([eventId], cancellationToken);
        if (ticketedEvent is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }

        // Clear the reconfirm policy
        ticketedEvent.SetReconfirmPolicy(null);
        
        return TypedResults.Ok();
    }
}