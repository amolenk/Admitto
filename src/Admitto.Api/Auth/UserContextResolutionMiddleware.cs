using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Api.Auth;

/// <summary>
/// Middleware that resolves the calling user's domain identity from JWT claims and caches it in
/// <see cref="HttpContext.Items"/> for the duration of the request.
///
/// Must be placed after <c>UseAuthentication()</c> and before <c>UseAuthorization()</c>.
/// Returns 403 for authenticated JWT requests whose identity cannot be resolved to a domain user.
/// API key requests are skipped — their identity is provided by <see cref="ApiKeyAuthenticationHandler"/>.
/// </summary>
public sealed class UserContextResolutionMiddleware(RequestDelegate next)
{
    internal const string UserContextItemKey = "user_context";

    public async Task InvokeAsync(HttpContext context, UserContextResolver resolver)
    {
        var user = context.User;

        // Only resolve JWT-authenticated requests.
        if (user.Identity?.IsAuthenticated == true
            && user.Identity.AuthenticationType != ApiKeyAuthenticationHandler.SchemeName)
        {
            if (!TryGetRouteScope(context, out var routeScope))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            var userContext = await resolver.ResolveAsync(user, routeScope, context.RequestAborted);
            if (userContext is null)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            context.Items[UserContextItemKey] = userContext;
        }

        await next(context);
    }

    private static bool TryGetRouteScope(HttpContext httpContext, out RouteScope routeScope)
    {
        routeScope = new RouteScope.Global();

        var teamIdValue = httpContext.GetRouteValue("teamId")?.ToString();
        var eventIdValue = httpContext.GetRouteValue("eventId")?.ToString();

        if (teamIdValue is null && eventIdValue is null)
            return true;

        if (teamIdValue is null)
            return false;

        var teamId = ParseTeamId(teamIdValue);
        if (teamId is not { } parsedTeamId)
            return false;

        if (eventIdValue is null)
        {
            routeScope = new RouteScope.Team(parsedTeamId);
            return true;
        }

        var eventId = ParseEventId(eventIdValue);
        if (eventId is not { } parsedEventId)
            return false;

        routeScope = new RouteScope.Event(parsedTeamId, parsedEventId);
        return true;
    }

    private static TeamId? ParseTeamId(string value)
    {
        if (!Guid.TryParse(value, out var id) || id == Guid.Empty)
            return null;

        return TeamId.From(id);
    }

    private static TicketedEventId? ParseEventId(string value)
    {
        if (!Guid.TryParse(value, out var id) || id == Guid.Empty)
            return null;

        return TicketedEventId.From(id);
    }
}
