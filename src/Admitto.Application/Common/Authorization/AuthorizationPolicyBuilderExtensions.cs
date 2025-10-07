using Microsoft.AspNetCore.Authorization;

namespace Amolenk.Admitto.Application.Common.Authorization;

// TODO Consider modeling email permissions separately.
public static class AuthorizationPolicyBuilderExtensions
{
    public static AuthorizationPolicyBuilder RequireAdmin(this AuthorizationPolicyBuilder builder)
    {
        builder.Requirements.Add(
            new AuthorizationRequirement((authService, _, context) =>
            {
                var userId = context.GetUserIdOrThrow();
                return authService.IsAdminAsync(userId);
            }));

        return builder;
    }

    public static AuthorizationPolicyBuilder RequireCanUpdateTeam(
        this AuthorizationPolicyBuilder builder,
        string teamSlugParameterName = "teamSlug")
    {
        builder.Requirements.Add(
            new AuthorizationRequirement(async (authService, slugResolver, context) =>
            {
                var userId = context.GetUserIdOrThrow();
                var teamId = await context.GetTeamIdOrThrowAsync(teamSlugParameterName, slugResolver);
                return await authService.CanUpdateTeamAsync(userId, teamId);
            }));

        return builder;
    }

    public static AuthorizationPolicyBuilder RequireCanViewTeam(
        this AuthorizationPolicyBuilder builder,
        string teamSlugParameterName = "teamSlug")
    {
        builder.Requirements.Add(
            new AuthorizationRequirement(async (authService, slugResolver, context) =>
            {
                var userId = context.GetUserIdOrThrow();
                var teamId = await context.GetTeamIdOrThrowAsync(teamSlugParameterName, slugResolver);
                return await authService.CanViewTeamAsync(userId, teamId);
            }));

        return builder;
    }

    public static AuthorizationPolicyBuilder RequireCanCreateEvent(
        this AuthorizationPolicyBuilder builder,
        string teamSlugParameterName = "teamSlug")
    {
        builder.Requirements.Add(
            new AuthorizationRequirement(async (authService, slugResolver, context) =>
            {
                var userId = context.GetUserIdOrThrow();
                var teamId = await context.GetTeamIdOrThrowAsync(teamSlugParameterName, slugResolver);
                return await authService.CanCreateEventAsync(userId, teamId);
            }));

        return builder;
    }

    public static AuthorizationPolicyBuilder RequireCanUpdateEvent(
        this AuthorizationPolicyBuilder builder,
        string teamSlugParameterName = "teamSlug",
        string eventSlugParameterName = "eventSlug")
    {
        builder.Requirements.Add(
            new AuthorizationRequirement(async (authService, slugResolver, context) =>
            {
                var userId = context.GetUserIdOrThrow();
                var (teamId, ticketedEventId) = await context.GetTeamAndTicketedEventIdOrThrowAsync(
                    teamSlugParameterName,
                    eventSlugParameterName,
                    slugResolver);

                return await authService.CanUpdateEventAsync(userId, teamId, ticketedEventId);
            }));

        return builder;
    }

    public static AuthorizationPolicyBuilder RequireCanViewEvent(
        this AuthorizationPolicyBuilder builder,
        string teamSlugParameterName = "teamSlug",
        string eventSlugParameterName = "eventSlug")
    {
        builder.Requirements.Add(
            new AuthorizationRequirement(async (authService, slugResolver, context) =>
            {
                var userId = context.GetUserIdOrThrow();
                var (teamId, ticketedEventId) = await context.GetTeamAndTicketedEventIdOrThrowAsync(
                    teamSlugParameterName,
                    eventSlugParameterName,
                    slugResolver);

                return await authService.CanViewEventAsync(userId, teamId, ticketedEventId);
            }));

        return builder;
    }

    private static Guid GetUserIdOrThrow(this HttpContext context)
    {
        var userId = context.User.GetUserId();
        if (userId is null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        return userId.Value;
    }

    private static async ValueTask<Guid> GetTeamIdOrThrowAsync(
        this HttpContext context,
        string teamSlugParameterName,
        ISlugResolver slugResolver)
    {
        var teamSlug = context.GetRouteValueOrThrow(teamSlugParameterName);
        var teamId = await slugResolver.ResolveTeamIdAsync(teamSlug);
        return teamId;
    }

    private static async ValueTask<(Guid TeamId, Guid TicketedEventId)> GetTeamAndTicketedEventIdOrThrowAsync(
        this HttpContext context,
        string teamSlugParameterName,
        string eventSlugParameterName,
        ISlugResolver slugResolver)
    {
        var teamSlug = context.GetRouteValueOrThrow(teamSlugParameterName);
        var eventSlug = context.GetRouteValueOrThrow(eventSlugParameterName);
        return await slugResolver.ResolveTeamAndTicketedEventIdsAsync(teamSlug, eventSlug);
    }

    private static string GetRouteValueOrThrow(this HttpContext context, string key)
    {
        var value = context.GetRouteValue(key);
        if (value is not string routeString)
        {
            throw new UnauthorizedAccessException($"Cannot authorize access because route parameter {key} is not set.");
        }

        return routeString;
    }
}