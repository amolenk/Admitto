using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Organization.Contracts;
using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Application;
using Amolenk.Admitto.Shared.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using TeamMemberRoleAuthorizationRequirement =
    Amolenk.Admitto.Shared.Application.Auth.TeamMemberRoleAuthorizationRequirement;

namespace Amolenk.Admitto.ApiService.Auth;

/// <summary>
/// Represents an authorization requirement that requires the user to be a team member with a given role.
/// Administrator users automatically satisfy this requirement.
/// </summary>
public class TeamMemberRoleAuthorizationHandler(
    IUserContextAccessor userContextAccessor,
    IOrganizationFacade organizationFacade,
    IAdministratorRoleService administratorRoleService,
    IOrganizationScopeResolver organizationScopeResolver)
    : AuthorizationHandler<TeamMemberRoleAuthorizationRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TeamMemberRoleAuthorizationRequirement requirement)
    {
        var userId = userContextAccessor.Current.UserId;

        // If the user is an administrator, they automatically satisfy the requirement.
        if (administratorRoleService.IsAdministrator(userId))
        {
            context.Succeed(requirement);
            return;
        }

        // Otherwise, check the user's role in the organization.
        var organizationScope = await organizationScopeResolver.ResolveAsync();
        var role = await organizationFacade.GetTeamMemberRoleAsync(userId, organizationScope.TeamId);

        if (role >= MapToTeamMemberRoleDto(requirement.RequiredRole))
        {
            context.Succeed(requirement);
        }
    }

    private static TeamMemberRoleDto MapToTeamMemberRoleDto(RequiredTeamMemberRole role) => role switch
    {
        RequiredTeamMemberRole.Crew => TeamMemberRoleDto.Crew,
        RequiredTeamMemberRole.Organizer => TeamMemberRoleDto.Organizer,
        RequiredTeamMemberRole.Owner => TeamMemberRoleDto.Owner,
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
}