using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.DenyVisaLetter;

/// <summary>
/// Represents the endpoint for denying a registration that requires a Visa letter.
/// </summary>
public static class DenyVisaLetterEndpoint
{
    public static RouteGroupBuilder MapDenyVisaLetter(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{attendeeId:guid}/deny-visa", DenyVisaLetter)
            .WithName(nameof(DenyVisaLetter))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok> DenyVisaLetter(
        [FromRoute] string teamSlug,
        [FromRoute] string eventSlug,
        [FromRoute] Guid attendeeId,
        [FromServices] ISlugResolver slugResolver,
        [FromServices] IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var eventId= await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        var ticketedEvent = await context.TicketedEvents.FindAsync([eventId], cancellationToken);
        if (ticketedEvent is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }
        
        var attendee = await context.Attendees.FindAsync([attendeeId], cancellationToken);
        if (attendee is null || attendee.TicketedEventId != eventId)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Attendee.NotFound);
        }
        
        attendee.CancelRegistration(ticketedEvent.StartsAt, CancellationReason.VisaLetterDenied);
        
        return TypedResults.Ok();
    }
}