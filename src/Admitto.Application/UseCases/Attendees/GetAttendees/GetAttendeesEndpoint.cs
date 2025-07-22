using Amolenk.Admitto.Application.Common.Authorization;

namespace Amolenk.Admitto.Application.UseCases.Attendees.GetAttendees;

public static class GetAttendeesEndpoint
{
    public static RouteGroupBuilder MapGetAttendees(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetAttendees)
            .WithName(nameof(GetAttendees))
            .RequireAuthorization(policy => policy.RequireCanViewEvent());

        return group;
    }

    private static async ValueTask<Ok<GetAttendeesResponse>> GetAttendees(
        string teamSlug,
        string eventSlug,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var teamId = await slugResolver.GetTeamIdAsync(teamSlug, cancellationToken);
        var eventId = await slugResolver.GetTicketedEventIdAsync(teamId, eventSlug, cancellationToken);
        
        var attendees = await context.Attendees
            .AsNoTracking()
            .Where(a => a.TeamId == teamId && a.TicketedEventId == eventId)
            .Select(a => new
            {
                a.Id,
                a.Email,
                a.FirstName,
                a.LastName,
                a.Status,
                a.LastChangedAt
            })
            .ToListAsync(cancellationToken: cancellationToken);
        
        var response = new GetAttendeesResponse(
            attendees
                .Select(a => new AttendeeDto(
                    a.Id,
                    a.Email,
                    a.FirstName,
                    a.LastName,
                    a.Status,
                    a.LastChangedAt))
                .ToArray());
        
        return TypedResults.Ok(response);
    }
}