using Amolenk.Admitto.ApiService.Auth;
using Amolenk.Admitto.Organization.Contracts;
using Amolenk.Admitto.Shared.Application.Auth;
using Amolenk.Admitto.Shared.Application.Http;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Authorization;

namespace Amolenk.Admitto.Api.Auth;

/// <summary>
/// Represents an authorization requirement that requires the user to be a team member with a given role.
/// Administrator users automatically satisfy this requirement.
/// </summary>
public class TeamMembershipAuthorizationHandler(
    IUserContextAccessor userContextAccessor,
    IOrganizationFacade organizationFacade,
    IAdministratorRoleService administratorRoleService,
    IOrganizationScopeResolver organizationScopeResolver)
    : AuthorizationHandler<TeamMembershipAuthorizationRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TeamMembershipAuthorizationRequirement requirement)
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
        var role = await organizationFacade.GetTeamMembershipRoleAsync(userId, organizationScope.TeamId);

        if (role.HasValue && MapToTeamMembershipRole(role.Value) >= requirement.RequiredRole)
        {
            context.Succeed(requirement);
        }
    }

    private static TeamMembershipRole MapToTeamMembershipRole(TeamMembershipRoleDto dto) => dto switch
    {
        TeamMembershipRoleDto.Crew => TeamMembershipRole.Crew,
        TeamMembershipRoleDto.Organizer => TeamMembershipRole.Organizer,
        TeamMembershipRoleDto.Owner => TeamMembershipRole.Owner,
        _ => throw new ArgumentOutOfRangeException(nameof(dto))
    };
}