using System.Security.Claims;
using Amolenk.Admitto.Api.Auth;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Contracts;

namespace Amolenk.Admitto.ApiService.Auth;

public class HttpContextUserContextAccessor(IHttpContextAccessor httpContextAccessor) : IUserContextAccessor
{
    private static readonly UserContextDto ApiKeyUser = new(
        Guid.Empty,
        "api-key",
        "apikey@system.local");

    public UserContextDto Current
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext;

            // No HTTP context — running in a background/hosted-service context.
            if (httpContext is null)
                return StaticUserContextAccessor.SystemUser;

            var user = httpContext.User;

            // API key requests have no human identity — return a fixed system user.
            if (user.Identity?.AuthenticationType == ApiKeyAuthenticationHandler.SchemeName)
                return ApiKeyUser;

            // The UserContextResolutionMiddleware pre-resolves and caches the domain user identity.
            if (httpContext.Items[UserContextResolutionMiddleware.UserContextItemKey] is UserContextDto cached)
                return cached;

            throw new InvalidOperationException(
                "User context has not been resolved. Ensure UserContextResolutionMiddleware is registered.");
        }
    }
}
