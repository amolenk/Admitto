using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.GetTeams.AdminApi;

/// <summary>
/// GET /admin/teams — returns all active teams for admins, or only the caller's teams for non-admins.
/// </summary>
public static class GetTeamsHttpEndpoint
{
    /// <summary>Maps the GET /admin/teams endpoint onto the provided route group.</summary>
    public static RouteGroupBuilder MapGetTeams(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetTeams)
            .WithName(nameof(GetTeams))
            .RequireAuthorization();

        return group;
    }

    private static async ValueTask<Ok<IReadOnlyList<TeamListItemDto>>> GetTeams(
        IUserContextAccessor userContextAccessor,
        IAdministratorRoleService administratorRoleService,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var callerId = userContextAccessor.Current.UserId;
        var callerIsAdmin = administratorRoleService.IsAdministrator(callerId);

        var query = new GetTeamsQuery(callerId, callerIsAdmin);
        var teams = await mediator.QueryAsync<GetTeamsQuery, IReadOnlyList<TeamListItemDto>>(
            query, cancellationToken);

        return TypedResults.Ok(teams);
    }
}
