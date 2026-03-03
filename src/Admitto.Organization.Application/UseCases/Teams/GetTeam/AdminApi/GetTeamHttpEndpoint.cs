using Amolenk.Admitto.Shared.Application.Auth;
using Amolenk.Admitto.Shared.Application.Http;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.Teams.GetTeam.AdminApi;

public static class GetTeamHttpEndpoint
{
    public static RouteGroupBuilder MapGetTeam(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetTeam)
            .WithName(nameof(GetTeam))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Crew));

        return group;
    }

    private static async ValueTask<Ok<TeamDto>> GetTeam(
        OrganizationScope organizationScope,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetTeamQuery(organizationScope.TeamId);

        var team = await mediator.QueryAsync<GetTeamQuery, TeamDto>(query, cancellationToken);

        return TypedResults.Ok(team);
    }
}