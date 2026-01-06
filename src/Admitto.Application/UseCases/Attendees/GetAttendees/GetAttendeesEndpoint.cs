using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.GetAttendees;

public static class GetAttendeesEndpoint
{
    public static RouteGroupBuilder MapGetAttendees(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetAttendees)
            .WithName(nameof(GetAttendees))
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Crew));

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
        
        var attendees = (await context.Attendees
            .AsNoTracking()
            .Where(a => a.TicketedEventId == eventId)
            .ToArrayAsync(cancellationToken))
            .Select(a => new AttendeeDto(
                a.Id,
                a.Email,
                a.FirstName,
                a.LastName,
                a.RegistrationStatus,
                a.AdditionalDetails
                    .Select(ad => new AdditionalDetailDto(ad.Name, ad.Value))
                    .ToArray(),
                a.Tickets
                    .Select(at => new TicketSelectionDto(at.TicketTypeSlug, at.Quantity))
                    .ToArray(),
                a.LastChangedAt))
            .ToArray();

        return TypedResults.Ok(new GetAttendeesResponse(attendees));
    }
}