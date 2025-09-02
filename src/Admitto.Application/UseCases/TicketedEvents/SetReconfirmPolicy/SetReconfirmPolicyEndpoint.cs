using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.SetReconfirmPolicy;

/// <summary>
/// Sets the reconfirm policy on a ticketed event.
/// </summary>
public static class SetReconfirmPolicyEndpoint
{
    public static RouteGroupBuilder MapSetReconfirmPolicy(this RouteGroupBuilder group)
    {
        group
            .MapPut("/{eventSlug}/policies/reconfirm", SetReconfirmPolicy)
            .WithName(nameof(SetReconfirmPolicy))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok> SetReconfirmPolicy(
        string teamSlug,
        string eventSlug,
        SetReconfirmPolicyRequest request,
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

        ticketedEvent.SetReconfirmPolicy(new ReconfirmPolicy(
            request.WindowStartBeforeEvent,
            request.WindowEndBeforeEvent,
            request.InitialDelayAfterRegistration,
            request.ReminderInterval));
        
        return TypedResults.Ok();
    }
}