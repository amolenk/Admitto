using Amolenk.Admitto.Core.Shared.Application.Auth;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.GetTeams.AdminApi;

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
        GetTeamsHandler handler,
        CancellationToken cancellationToken)
    {
        var callerId = userContextAccessor.Current.UserId;
        var callerIsAdmin = await administratorRoleService.IsAdministratorAsync(callerId, cancellationToken);

        var teams = await handler.HandleAsync(new GetTeamsQuery(callerId, callerIsAdmin), cancellationToken);

        return TypedResults.Ok(teams);
    }
}
