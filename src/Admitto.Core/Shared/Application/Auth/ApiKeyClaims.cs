using System.Security.Claims;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Shared.Application.Auth;

public static class ApiKeyClaims
{
    public const string TeamIdClaimType = "team_id";

    public static Guid GetRequiredTeamId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(TeamIdClaimType);
        if (claim is null || !Guid.TryParse(claim.Value, out var teamId))
        {
            throw new BusinessRuleViolationException(Errors.TeamScopeMissing);
        }

        return teamId;
    }

    private static class Errors
    {
        public static readonly Error TeamScopeMissing = new(
            "api_key.team_scope_missing",
            "The API key team scope is missing or invalid.",
            Type: ErrorType.Unauthorized);
    }
}
