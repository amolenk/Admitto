using Amolenk.Admitto.Application.Common.Authorization;

namespace Amolenk.Admitto.Application.UseCases.Registrations.GetRegistrations;

public static class GetRegistrationsEndpoint
{
    public static RouteGroupBuilder MapGetRegistrations(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetRegistrations)
            .WithName(nameof(GetRegistrations))
            .RequireAuthorization(policy => policy.RequireCanViewEvent());

        return group;
    }

    private static async ValueTask<Ok<GetRegistrationsResponse>> GetRegistrations(
        string teamSlug,
        string eventSlug,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var teamId = await slugResolver.GetTeamIdAsync(teamSlug, cancellationToken);
        var eventId = await slugResolver.GetTicketedEventIdAsync(teamId, eventSlug, cancellationToken);
        
        var attendees = await context.Registrations
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
        
        var response = new GetRegistrationsResponse(
            attendees
                .Select(a => new RegistrationDto(
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