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
        IDomainContext context,
        CancellationToken cancellationToken)
    {
        // TODO Introduce slug resolver
        var ids = await context.GetTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);
        
        var attendees = await context.Attendees
            .AsNoTracking()
            .Where(a => a.TeamId == ids.TeamId && a.TicketedEventId == ids.TicketedEventId)
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