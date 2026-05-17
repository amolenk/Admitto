using Amolenk.Admitto.Core.Shared.Application.Auth;
using Microsoft.AspNetCore.Authorization;

namespace Amolenk.Admitto.Api.Auth;

/// <summary>
/// Authorization handler for <see cref="AdminAuthorizationRequirement"/>.
/// </summary>
public class AdminAuthorizationHandler(IUserContextAccessor userContextAccessor)
    : AuthorizationHandler<AdminAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminAuthorizationRequirement requirement)
    {
        if (userContextAccessor.Current.IsAdmin)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
