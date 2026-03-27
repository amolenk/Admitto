using System.Security.Claims;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Contracts;

namespace Amolenk.Admitto.ApiService.Auth;

public class HttpContextUserContextAccessor(IHttpContextAccessor httpContextAccessor) : IUserContextAccessor
{
    public UserContextDto Current
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext
                              ?? throw new InvalidOperationException("No HTTP context available.");

            var user = httpContext.User;
        
            // TODO
            return new UserContextDto(GetUserId(user), GetUserName(user) ?? "Unknown", "todo@example.com");
        }
    }
    
    private static Guid GetUserId(ClaimsPrincipal user)
    {
        // Entra
        var objectIdentifierClaim = user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier");
        if (objectIdentifierClaim is not null) return Guid.Parse(objectIdentifierClaim.Value);

        // Keycloak
        var nameClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (nameClaim is not null && Guid.TryParse(nameClaim.Value, out var userId))
        {
            return userId;
        }

        throw new ArgumentException(
            "Cannot find user ID in principal. Ensure the user is authenticated and has the correct claims.");
    }
    //
    // public string? GetUserEmail()
    // {
    //     var claim = user.FindFirst(ClaimTypes.Email);
    //     if (claim is not null) return claim.Value;
    //
    //     claim = user.FindFirst("preferred_username");
    //     if (claim is not null) return claim.Value;
    //
    //     throw new ArgumentException(
    //         "Cannot find user email in principal. Ensure the user is authenticated and has the correct claims.");
    // }

    private static string? GetUserName(ClaimsPrincipal user)
    {
        var claim = user.FindFirst("name");
        if (claim is not null) return claim.Value;

        throw new ArgumentException(
            "Cannot find user name in principal. Ensure the user is authenticated and has the correct claims.");
    }
}