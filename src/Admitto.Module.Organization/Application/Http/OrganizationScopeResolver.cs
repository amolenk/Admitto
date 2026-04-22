using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Http;

namespace Amolenk.Admitto.Module.Organization.Application.Http;

public sealed class OrganizationScopeResolver(
    IOrganizationFacade organizationFacade,
    ITicketedEventIdLookup ticketedEventIdLookup)
    : IOrganizationScopeResolver
{
    private OrganizationScope? _cachedScope;

    public async ValueTask<OrganizationScope> ResolveAsync(
        string teamSlug,
        string? eventSlug = null,
        CancellationToken cancellationToken = default)
    {
        if (_cachedScope is not null) return _cachedScope;

        var teamId = await organizationFacade.GetTeamIdAsync(teamSlug, cancellationToken);

        Guid? eventId = null;
        if (eventSlug is not null)
        {
            eventId = await ticketedEventIdLookup.GetTicketedEventIdAsync(teamId, eventSlug, cancellationToken);
        }

        _cachedScope = new OrganizationScope(teamSlug, teamId, eventSlug, eventId);
        return _cachedScope;
    }
}
