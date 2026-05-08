using Amolenk.Admitto.Api.Auth;
using Amolenk.Admitto.Module.Organization.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Amolenk.Admitto.Api.Endpoints;

/// <summary>
/// Verifies that the API key's team matches the {teamId} in the request path.
/// Returns 403 if there is a mismatch.
/// </summary>
public class ApiKeyTeamScopeFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        var teamIdValue = httpContext.GetRouteValue("teamId")?.ToString();
        if (!Guid.TryParse(teamIdValue, out var routeTeamId))
        {
            return await next(context);
        }

        var teamIdClaim = httpContext.User.FindFirst(ApiKeyAuthenticationHandler.TeamIdClaimType);
        if (teamIdClaim is null || !Guid.TryParse(teamIdClaim.Value, out var claimTeamId))
        {
            return await next(context);
        }

        if (routeTeamId != claimTeamId)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "The API key does not belong to the requested team.");
        }

        return await next(context);
    }
}
