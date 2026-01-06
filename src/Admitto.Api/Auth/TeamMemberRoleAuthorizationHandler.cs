using Amolenk.Admitto.Application.Common.Authentication;
using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Application.Common.Slugs;
using Microsoft.AspNetCore.Authorization;

namespace Amolenk.Admitto.ApiService.Auth;

/// <summary>
/// Represents an authorization requirement that requires the user to be a team member with a given role.
/// Administrator users automatically satisfy this requirement.
/// </summary>
public class TeamMemberRoleAuthorizationHandler(
    IHttpContextAccessor contextAccessor,
    ISlugResolver slugResolver,
    ITeamMemberRoleService teamMemberRoleService,
    IAdministratorRoleService administratorRoleService)
    : AuthorizationHandler<TeamMemberRoleAuthorizationRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TeamMemberRoleAuthorizationRequirement requirement)
    {
        var httpContext = contextAccessor.HttpContext ??
                          throw new InvalidOperationException("HttpContext is required.");

        var userId = context.User.GetUserId();

        if (administratorRoleService.IsAdministrator(userId))
        {
            context.Succeed(requirement);
            return;
        }
        
        var teamId = await GetTeamIdOrThrowAsync(httpContext, requirement.TeamSlugParameterName, slugResolver);
        var role = await teamMemberRoleService.GetTeamMemberRoleAsync(userId, teamId);
        
        if (role is not null && role >= requirement.RequiredRole)
        {
            context.Succeed(requirement);
        }
    }

    private static async ValueTask<Guid> GetTeamIdOrThrowAsync(
        HttpContext context,
        string teamSlugParameterName,
        ISlugResolver slugResolver)
    {
        var teamSlug = GetRouteValueOrThrow(context, teamSlugParameterName);
        var teamId = await slugResolver.ResolveTeamIdAsync(teamSlug);
        return teamId;
    }

    private static string GetRouteValueOrThrow(HttpContext context, string key)
    {
        var value = context.GetRouteValue(key);
        if (value is not string routeString)
        {
            throw new UnauthorizedAccessException($"Cannot authorize access because route parameter {key} is not set.");
        }

        return routeString;
    }
}