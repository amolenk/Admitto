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
            // Parse route context.
            var teamId = TryParseTeamId(context);
            var eventId = TryParseEventId(context);

            var userContext = await resolver.ResolveAsync(user, teamId, eventId, context.RequestAborted);
            if (userContext is null)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            context.Items[UserContextItemKey] = userContext;
        }

        await next(context);
    }

    private static TeamId? TryParseTeamId(HttpContext httpContext)
    {
        var v = httpContext.GetRouteValue("teamId")?.ToString();
        return v is null ? null : TeamId.Parse(v);
    }

    private static TicketedEventId? TryParseEventId(HttpContext httpContext)
    {
        var v = httpContext.GetRouteValue("eventId")?.ToString();
        return v is null ? null : TicketedEventId.Parse(v);
    }
}
