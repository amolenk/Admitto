using System.Security.Claims;

namespace Amolenk.Admitto.Application.Common.Authorization;

public static class AuthorizationExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        // TODO Do role mapping to sub or something like that.
        // TODO This is only tested with local Keycloak
        // var userId = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is not null) return Guid.Parse(userIdClaim.Value);

        return null;
    }
    
    public static string GetRouteValue2(this HttpContext context, string key)
    {
        var value = context.GetRouteValue(key);
        if (value is not string routeString)
        {
            throw new ArgumentException($"{key} not found in route values.");
        }

        return routeString;
    }
}