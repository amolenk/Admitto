using Amolenk.Admitto.ApiService.Auth;
using Amolenk.Admitto.Core.Organization.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
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
    IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<TeamMembershipAuthorizationRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TeamMembershipAuthorizationRequirement requirement)
    {
        var userId = userContextAccessor.Current.UserId;

        // If the user is an administrator, they automatically satisfy the requirement.
        if (await administratorRoleService.IsAdministratorAsync(userId))
        {
            context.Succeed(requirement);
            return;
        }

        // Extract teamId from route values since authorization runs before endpoint binding.
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        var teamIdValue = httpContext.GetRouteValue("teamId")?.ToString();
        if (!Guid.TryParse(teamIdValue, out var teamId))
        {
            return;
        }

        var role = await organizationFacade.GetTeamMembershipRoleAsync(userId, teamId);

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