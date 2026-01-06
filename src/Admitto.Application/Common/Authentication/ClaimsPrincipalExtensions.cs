using System.Security.Claims;

namespace Amolenk.Admitto.Application.Common.Authentication;

public static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal user)
    {
        public Guid GetUserId()
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

        public string? GetUserEmail()
        {
            var claim = user.FindFirst(ClaimTypes.Email);
            if (claim is not null) return claim.Value;

            claim = user.FindFirst("preferred_username");
            if (claim is not null) return claim.Value;

            throw new ArgumentException(
                "Cannot find user email in principal. Ensure the user is authenticated and has the correct claims.");
        }

        public string? GetUserName()
        {
            var claim = user.FindFirst("name");
            if (claim is not null) return claim.Value;

            throw new ArgumentException(
                "Cannot find user name in principal. Ensure the user is authenticated and has the correct claims.");
        }
    }
}