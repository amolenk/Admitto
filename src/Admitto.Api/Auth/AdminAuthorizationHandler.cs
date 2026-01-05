using Amolenk.Admitto.Application.Common.Authentication;
using Amolenk.Admitto.Application.Common.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Amolenk.Admitto.ApiService.Auth;

/// <summary>
/// Authorization handler for <see cref="AdminAuthorizationRequirement"/>.
/// </summary>
public class AdminAuthorizationHandler(IAdministratorRoleService administratorRoleService)
    : AuthorizationHandler<AdminAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminAuthorizationRequirement requirement)
    {
        var userId = context.User.GetUserId();
        if (administratorRoleService.IsAdministrator(userId))
        {
            context.Succeed(requirement);
        }
        
        return Task.CompletedTask;
    }
}