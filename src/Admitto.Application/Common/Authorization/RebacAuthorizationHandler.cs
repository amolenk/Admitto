using Microsoft.AspNetCore.Authorization;

namespace Amolenk.Admitto.Application.Common.Authorization;

public class RebacAuthorizationHandler(IHttpContextAccessor httpContextAccessor,
    IRebacAuthorizationService authorizationService ) 
    : AuthorizationHandler<RebacAuthorizationRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, 
        RebacAuthorizationRequirement requirement)
    {
        // TODO Do role mapping to sub or something like that.
        // TODO This is only tested with local Keycloak
        var userId = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        if (!Guid.TryParse(userId, out var userIdGuid))
        {
            return;
        }

        var allowed = await authorizationService.CheckAsync(userIdGuid, requirement.Relation, requirement.ObjectType,
            requirement.GetObjectId(httpContextAccessor.HttpContext!));
        
        if (allowed)
        {
            context.Succeed(requirement);
        }
    }
}