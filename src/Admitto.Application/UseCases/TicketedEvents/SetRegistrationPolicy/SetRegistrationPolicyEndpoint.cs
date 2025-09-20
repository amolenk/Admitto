using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.SetRegistrationPolicy;

/// <summary>
/// Sets the registration policy on a ticketed event.
/// </summary>
public static class SetRegistrationPolicyEndpoint
{
    public static RouteGroupBuilder MapSetRegistrationPolicy(this RouteGroupBuilder group)
    {
        group
            .MapPut("/{eventSlug}/policies/reconfirm", SetRegistrationPolicy)
            .WithName(nameof(SetRegistrationPolicy))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok> SetRegistrationPolicy(
        string teamSlug,
        string eventSlug,
        SetRegistrationPolicyRequest request,
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

        ticketedEvent.SetRegistrationPolicy(
            new RegistrationPolicy(
                request.OpensBeforeEvent,
                request.ClosesBeforeEvent,
                request.EmailDomainName));
        
        return TypedResults.Ok();
    }
}