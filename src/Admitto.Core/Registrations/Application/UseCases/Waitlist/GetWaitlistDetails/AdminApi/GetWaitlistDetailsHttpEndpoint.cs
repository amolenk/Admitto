using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlist.GetWaitlistDetails.AdminApi;

public static class GetWaitlistDetailsHttpEndpoint
{
    public static RouteGroupBuilder MapGetWaitlistDetails(this RouteGroupBuilder group)
    {
        group
            .MapGet("/waitlist", HandleAsync)
            .WithName(nameof(GetWaitlistDetailsHttpEndpoint))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Results<Ok<WaitlistDetailsDto>, NotFound>> HandleAsync(
        Guid teamId,
        Guid eventId,
        Guid ticketTypeId,
        IQueryHandler<GetWaitlistDetailsQuery, WaitlistDetailsDto?> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetWaitlistDetailsQuery(eventId, ticketTypeId);
        var result = await handler.HandleAsync(query, cancellationToken);

        return result is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(result);
    }
}
