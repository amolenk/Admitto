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
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);
        
        var attendees = await context.Attendees
            .AsNoTracking()
            .Where(a => a.TicketedEventId == eventId)
            .Select(a => new AttendeeDto(
                a.Id,
                a.Email,
                a.FirstName,
                a.LastName,
                a.RegistrationStatus,
                a.LastChangedAt))
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(new GetAttendeesResponse(attendees));
    }
}