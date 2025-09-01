using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Cryptography;

namespace Amolenk.Admitto.Application.UseCases.Attendees.CancelRegistration;

/// <summary>
/// Represents the endpoint for cancelling an existing registration for a ticketed event.
/// </summary>
public static class CancelRegistrationEndpoint
{
    public static RouteGroupBuilder MapCancelRegistration(this RouteGroupBuilder group)
    {
        group
            .MapDelete("/{attendeeId:guid}", CancelRegistration)
            .WithName(nameof(CancelRegistration))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok> CancelRegistration(
        string teamSlug,
        string eventSlug,
        Guid attendeeId,
        string signature,
        ISigningService signingService,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var eventId= await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        if (!await signingService.IsValidAsync(attendeeId, signature, eventId, cancellationToken))
        {
            throw new ApplicationRuleException(ApplicationRuleError.Signing.InvalidSignature);
        }
        
        var attendee = await context.Attendees.FindAsync([attendeeId], cancellationToken);
        if (attendee is null || attendee.TicketedEventId != eventId)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Attendee.NotFound);
        }        
        
        var ticketedEvent = await context.TicketedEvents.FindAsync([eventId], cancellationToken);
        
        attendee.CancelRegistration(ticketedEvent!.CancellationPolicy, ticketedEvent.StartTime);

        context.Attendees.Remove(attendee);
        
        return TypedResults.Ok();
    }
}