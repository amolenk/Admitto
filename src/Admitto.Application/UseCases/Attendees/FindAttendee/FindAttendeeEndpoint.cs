using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.FindAttendee;

public static class FindAttendeeEndpoint
{
    public static RouteGroupBuilder MapFindAttendee(this RouteGroupBuilder group)
    {
        group
            .MapGet("/by-email", FindAttendee)
            .WithName(nameof(FindAttendee))
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Crew));

        return group;
    }

    private static async ValueTask<Results<Ok<FindAttendeeResponse>, NotFound>> FindAttendee(
        [FromRoute] string teamSlug,
        [FromRoute] string eventSlug,
        [FromQuery] string email,
        [FromServices] ISlugResolver slugResolver,
        [FromServices] IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);
        
        var participant = await context.ParticipationView
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.TicketedEventId == eventId && p.Email == email,
                cancellationToken);

        if (participant?.AttendeeId is null)
        {
            return TypedResults.NotFound();
        }

        var response = new FindAttendeeResponse(
            participant.PublicId,
            participant.AttendeeId.Value);

        return TypedResults.Ok(response);
    }
}