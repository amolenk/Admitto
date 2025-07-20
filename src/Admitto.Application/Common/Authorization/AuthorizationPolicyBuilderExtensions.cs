using Microsoft.AspNetCore.Authorization;

namespace Amolenk.Admitto.Application.Common.Authorization;

public static class AuthorizationPolicyBuilderExtensions
{
    public static AuthorizationPolicyBuilder RequireCanCreateTeam(this AuthorizationPolicyBuilder builder)
    {
        builder.Requirements.Add(new AuthorizationRequirement((authService, context) =>
        {
            var userId = context.GetUserIdOrThrow();
            return authService.CanCreateTeamAsync(userId);
        }));
        
        return builder;
    }

    public static AuthorizationPolicyBuilder RequireCanUpdateTeam(this AuthorizationPolicyBuilder builder, 
        string teamSlugParameterName = "teamSlug")
    {
        builder.Requirements.Add(new AuthorizationRequirement((authService, context) =>
        {
            var userId = context.GetUserIdOrThrow();
            var teamSlug = context.GetRouteValueOrThrow(teamSlugParameterName);
            return authService.CanUpdateTeamAsync(userId, teamSlug);
        }));
        
        return builder;
    }
    
    public static AuthorizationPolicyBuilder RequireCanViewTeam(this AuthorizationPolicyBuilder builder, 
        string teamSlugParameterName = "teamSlug")
    {
        builder.Requirements.Add(new AuthorizationRequirement((authService, context) =>
        {
            var userId = context.GetUserIdOrThrow();
            var teamSlug = context.GetRouteValueOrThrow(teamSlugParameterName);
            return authService.CanViewTeamAsync(userId, teamSlug);
        }));
        
        return builder;
    }
    
    public static AuthorizationPolicyBuilder RequireCanCreateEvent(this AuthorizationPolicyBuilder builder, 
        string teamSlugParameterName = "teamSlug")
    {
        builder.Requirements.Add(new AuthorizationRequirement((authService, context) =>
        {
            var userId = context.GetUserIdOrThrow();
            var teamSlug = context.GetRouteValueOrThrow(teamSlugParameterName);
            return authService.CanCreateEventAsync(userId, teamSlug);
        }));
        
        return builder;
    }
    
    public static AuthorizationPolicyBuilder RequireCanUpdateEvent(this AuthorizationPolicyBuilder builder, 
        string teamSlugParameterName = "teamSlug", string eventSlugParameterName = "eventSlug")
    {
        builder.Requirements.Add(new AuthorizationRequirement((authService, context) =>
        {
            var userId = context.GetUserIdOrThrow();
            var teamSlug = context.GetRouteValueOrThrow(teamSlugParameterName);
            var eventSlug = context.GetRouteValueOrThrow(eventSlugParameterName);
            return authService.CanUpdateEventAsync(userId, teamSlug, eventSlug);
        }));
        
        return builder;
    }
    
    public static AuthorizationPolicyBuilder RequireCanViewEvent(this AuthorizationPolicyBuilder builder, 
        string teamSlugParameterName = "teamSlug", string eventSlugParameterName = "eventSlug")
    {
        builder.Requirements.Add(new AuthorizationRequirement((authService, context) =>
        {
            var userId = context.GetUserIdOrThrow();
            var teamSlug = context.GetRouteValueOrThrow(teamSlugParameterName);
            var eventSlug = context.GetRouteValueOrThrow(eventSlugParameterName);
            return authService.CanViewEventAsync(userId, teamSlug, eventSlug);
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
