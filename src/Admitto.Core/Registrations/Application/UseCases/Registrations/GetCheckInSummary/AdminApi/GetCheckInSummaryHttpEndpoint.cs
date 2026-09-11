using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetCheckInSummary.AdminApi;

public static class GetCheckInSummaryHttpEndpoint
{
    public static RouteGroupBuilder MapGetCheckInSummary(this RouteGroupBuilder group)
    {
        group
            .MapGet("/check-in/summary", GetCheckInSummary)
            .WithName(nameof(GetCheckInSummary))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Crew));

        return group;
    }

    private static async ValueTask<Results<Ok<CheckInSummaryDto>, NotFound>> GetCheckInSummary(
        Guid teamId,
        Guid eventId,
        IQueryHandler<GetCheckInSummaryQuery, CheckInSummaryDto?> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetCheckInSummaryQuery(teamId, eventId),
            cancellationToken);

        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
