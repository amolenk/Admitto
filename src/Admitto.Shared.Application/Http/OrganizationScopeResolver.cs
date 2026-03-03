using Amolenk.Admitto.Organization.Contracts;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Shared.Application.Http;

public interface IOrganizationScopeResolver
{
    ValueTask<OrganizationScope> ResolveAsync(CancellationToken cancellationToken = default);
}

public sealed class OrganizationScopeResolver(
    IHttpContextAccessor httpContextAccessor,
    IOrganizationFacade organizationFacade)
    : IOrganizationScopeResolver
{
    private OrganizationScope? _cachedScope;

    public async ValueTask<OrganizationScope> ResolveAsync(CancellationToken cancellationToken)
    {
        if (_cachedScope is not null) return _cachedScope;

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            throw new InvalidOperationException("No HttpContext available.");
        }

        var teamSlug = GetRouteRequired(httpContext, "teamSlug");
        var eventSlug = GetRouteOptional(httpContext, "eventSlug");

        var teamId = await organizationFacade.GetTeamIdAsync(teamSlug, cancellationToken);

        var eventId = Guid.Empty;
        if (eventSlug is not null)
        {
            eventId = await organizationFacade.GetTicketedEventIdAsync(teamId, eventSlug, cancellationToken);
        }

        _cachedScope = new OrganizationScope(teamSlug, teamId, eventSlug, eventId);
        return _cachedScope;
    }

    private static string GetRouteRequired(HttpContext ctx, string key)
    {
        if (ctx.Request.RouteValues.TryGetValue(key, out var value) &&
            value is not null &&
            !string.IsNullOrWhiteSpace(value.ToString()))
        {
            return value.ToString()!;
        }

        throw new BadHttpRequestException($"Missing route parameter '{key}'.");
    }

    private static string? GetRouteOptional(HttpContext ctx, string key)
        => ctx.Request.RouteValues.TryGetValue(key, out var value) ? value?.ToString() : null;
}