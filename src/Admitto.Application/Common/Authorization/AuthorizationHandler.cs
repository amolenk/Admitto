using Microsoft.AspNetCore.Authorization;
using IAuthorizationService = Amolenk.Admitto.Application.Common.Abstractions.IAuthorizationService;

namespace Amolenk.Admitto.Application.Common.Authorization;

// TODO Can be moved out of the Application project?
public class AuthorizationHandler(IAuthorizationService authorizationService, IHttpContextAccessor httpContextAccessor) 
    : AuthorizationHandler<AuthorizationRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, 
        AuthorizationRequirement requirement)
    {
        var allowed = await requirement.Check(authorizationService, httpContextAccessor.HttpContext!);
        if (allowed)
        {
            context.Succeed(requirement);
        }
        
        // TODO If not allowed, in some cases we might want to throw a not-found exception.
        // TODO Maybe we can pass a flag to the requirement to indicate this?
    }
}