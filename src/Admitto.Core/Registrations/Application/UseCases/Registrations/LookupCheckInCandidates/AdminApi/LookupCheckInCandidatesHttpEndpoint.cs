using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.LookupCheckInCandidates.AdminApi;

public static class LookupCheckInCandidatesHttpEndpoint
{
    public static RouteGroupBuilder MapLookupCheckInCandidates(this RouteGroupBuilder group)
    {
        group
            .MapGet("/check-in/lookup", LookupCheckInCandidates)
            .WithName(nameof(LookupCheckInCandidates))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Crew));

        return group;
    }

    private static async ValueTask<Ok<IReadOnlyList<CheckInLookupCandidateDto>>> LookupCheckInCandidates(
        Guid teamId,
        Guid eventId,
        string? query,
        IQueryHandler<LookupCheckInCandidatesQuery, IReadOnlyList<CheckInLookupCandidateDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new LookupCheckInCandidatesQuery(teamId, eventId, query ?? string.Empty),
            cancellationToken);

        return TypedResults.Ok(result);
    }
}
