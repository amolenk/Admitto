using Amolenk.Admitto.Shared.Application.Auth;
using Microsoft.AspNetCore.Authorization;

namespace Amolenk.Admitto.ApiService.Auth;

/// <summary>
/// Authorization handler for <see cref="AdminAuthorizationRequirement"/>.
/// </summary>
public class AdminAuthorizationHandler(
    IUserContextAccessor userContextAccessor,
    IAdministratorRoleService administratorRoleService)
    : AuthorizationHandler<AdminAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminAuthorizationRequirement requirement)
    {
        var userId = userContextAccessor.Current.UserId;
        if (administratorRoleService.IsAdministrator(userId))
        {
            context.Succeed(requirement);
        }
        
        return Task.CompletedTask;
    }
}