using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.GetTeam.AdminApi;

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