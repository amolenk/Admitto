using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Contracts;

namespace Amolenk.Admitto.Api.Auth;

public class HttpContextUserContextAccessor(IHttpContextAccessor httpContextAccessor) : IUserContextAccessor
{
    public UserContextDto Current
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext;

            // No HTTP context — running in a background/hosted-service context.
            if (httpContext is null)
                return StaticUserContextAccessor.SystemUser;

            var user = httpContext.User;

            // Anonymous public routes (for example, the shared scanner) have no signed-in
            // or API-key identity. Attribute their writes to a generic system identity rather
            // than inferring an individual identity that was never authenticated.
            if (user.Identity?.IsAuthenticated != true)
                return StaticUserContextAccessor.SystemUser;

            // API key requests have no human identity — build a system user scoped to the key's team.
            if (user.Identity?.AuthenticationType == ApiKeyAuthenticationHandler.SchemeName)
                return BuildApiKeyUserContext(user);

            // The UserContextResolutionMiddleware pre-resolves and caches the domain user identity.
            if (httpContext.Items[UserContextResolutionMiddleware.UserContextItemKey] is UserContextDto cached)
                return cached;

            throw new InvalidOperationException(
                "User context has not been resolved. Ensure UserContextResolutionMiddleware is registered.");
        }
    }

    private static UserContextDto BuildApiKeyUserContext(System.Security.Claims.ClaimsPrincipal user)
    {
        var teamIdClaim = user.FindFirst(ApiKeyClaims.TeamIdClaimType);
        if (teamIdClaim is not null && Guid.TryParse(teamIdClaim.Value, out var teamId))
        {
            return new UserContextDto(teamId, $"api-key-{teamId}", $"{teamId}@apikey.admitto");
        }

        return new UserContextDto(Guid.Empty, "api-key", "unknown@apikey.admitto");
    }
}
