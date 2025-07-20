using Amolenk.Admitto.Application.Common.Authorization;

namespace Amolenk.Admitto.Application.UseCases.PendingRegistrations.GetPendingRegistrations;

public static class GetPendingRegistrationsEndpoint
{
    public static RouteGroupBuilder MapGetPendingRegistrations(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetPendingRegistrations)
            .WithName(nameof(GetPendingRegistrations))
            .RequireAuthorization(policy => policy.RequireCanViewEvent());

        return group;
    }

    private static async ValueTask<Ok<GetPendingRegistrationsResponse>> GetPendingRegistrations(
        string teamSlug,
        string eventSlug,
        IDomainContext context,
        CancellationToken cancellationToken)
    {
        var teamId = await context.Teams.GetTeamIdAsync(teamSlug, cancellationToken);

        var registrationRequests = await context.TicketedEvents
            .AsNoTracking()
            .Join(
                context.PendingRegistrations,
                e => e.Id,
                r => r.TicketedEventId,
                (e, r) => new { Event = e, Registration = r })
            .Where(x => x.Event.TeamId == teamId && x.Event.Slug == eventSlug)
            .Select(x => new
            {
                x.Registration.Id,
                x.Registration.Email,
                x.Registration.FirstName,
                x.Registration.LastName,
                x.Registration.Status,
                x.Registration.LastChangedAt
            })
            .ToListAsync(cancellationToken: cancellationToken);
        
        var response = new GetPendingRegistrationsResponse(
            registrationRequests
                .Select(r => new PendingRegistrationDto(
                    r.Id,
                    r.Email,
                    r.FirstName,
                    r.LastName,
                    r.Status,
                    r.LastChangedAt))
                .ToArray());
        
        return TypedResults.Ok(response);
    }
}