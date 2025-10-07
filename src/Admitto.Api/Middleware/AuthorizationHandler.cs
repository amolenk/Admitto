using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Application.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using IAuthorizationService = Amolenk.Admitto.Application.Common.Abstractions.IAuthorizationService;

namespace Amolenk.Admitto.ApiService.Middleware;

public class AuthorizationHandler(
    IAuthorizationService authorizationService,
    IHttpContextAccessor httpContextAccessor,
    ISlugResolver slugResolver)
    : AuthorizationHandler<AuthorizationRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AuthorizationRequirement requirement)
    {
        var allowed = await requirement.Check(authorizationService, slugResolver, httpContextAccessor.HttpContext!);
        if (allowed)
        {
            context.Succeed(requirement);
        }
    }
}