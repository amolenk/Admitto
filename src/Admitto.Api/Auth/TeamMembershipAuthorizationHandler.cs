using Amolenk.Admitto.Core.Shared.Application.Auth;
using Microsoft.AspNetCore.Authorization;

namespace Amolenk.Admitto.Api.Auth;

/// <summary>
/// Represents an authorization requirement that requires the user to be a team member with a given role.
/// Administrator users automatically satisfy this requirement.
/// </summary>
public class TeamMembershipAuthorizationHandler(IUserContextAccessor userContextAccessor)
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

        // Otherwise, the user must have a team membership with a sufficient role.
        if (userContext.Role >= requirement.RequiredRole)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
