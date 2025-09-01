using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Cryptography;

namespace Amolenk.Admitto.Application.UseCases.Attendees.GetAttendee;

public static class GetAttendeeEndpoint
{
    public static RouteGroupBuilder MapGetAttendee(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{attendeeId:guid}", GetAttendee)
            .WithName(nameof(GetAttendee))
            .RequireAuthorization(policy => policy.RequireCanViewEvent());

        return group;
    }

    private static async ValueTask<Ok<GetAttendeeResponse>> GetAttendee(
        string teamSlug,
        string eventSlug,
        Guid attendeeId,
        ISlugResolver slugResolver,
        IApplicationContext context,
        ISigningService signingService,
        CancellationToken cancellationToken)
    {
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        var attendee = await context.Attendees.FindAsync([attendeeId], cancellationToken);
        if (attendee is null || attendee.TicketedEventId != eventId)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Attendee.NotFound);
        }

        var signature = await signingService.SignAsync(attendeeId, eventId, cancellationToken);
        
        var response = new GetAttendeeResponse(
            attendee.Id,
            attendee.Email,
            attendee.FirstName,
            attendee.LastName,
            attendee.RegistrationStatus, 
            signature);
        
        return TypedResults.Ok(response);
    }
}