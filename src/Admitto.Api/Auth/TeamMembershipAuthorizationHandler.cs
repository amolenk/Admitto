using Amolenk.Admitto.Core.Shared.Application.Auth;
using Microsoft.AspNetCore.Authorization;

namespace Amolenk.Admitto.Api.Auth;

/// <summary>
/// Represents an authorization requirement that requires the user to be a team member with a given role.
/// Administrator users automatically satisfy this requirement.
/// </summary>
public class TeamMembershipAuthorizationHandler(
    IUserContextAccessor userContextAccessor,
    IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<TeamMembershipAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TeamMembershipAuthorizationRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        var userContext = userContextAccessor.Current;

        // Administrators automatically satisfy any team membership requirement.
        if (userContext.IsAdmin)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Extract teamId from route values since authorization runs before endpoint binding.
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
            return Task.CompletedTask;

        var teamIdValue = httpContext.GetRouteValue("teamId")?.ToString();
        if (!Guid.TryParse(teamIdValue, out var teamId))
            return Task.CompletedTask;

        var membership = userContext.TeamMemberships?.FirstOrDefault(m => m.TeamId == teamId);
        if (membership is not null && membership.Role >= requirement.RequiredRole)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
