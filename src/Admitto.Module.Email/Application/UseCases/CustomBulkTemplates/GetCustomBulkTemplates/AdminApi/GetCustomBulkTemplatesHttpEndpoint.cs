using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.GetCustomBulkTemplates.AdminApi;

public static class GetCustomBulkTemplatesHttpEndpoint
{
    public static RouteGroupBuilder MapGetCustomBulkTemplates(
        this RouteGroupBuilder group,
        EmailSettingsScope scope,
        Func<OrganizationScope, Guid> scopeIdSelector)
    {
        var endpointName = scope == EmailSettingsScope.Team
            ? "GetTeamCustomBulkTemplates"
            : "GetEventCustomBulkTemplates";

        var handler = new Handler(scope, scopeIdSelector);

        group
            .MapGet("/", handler.HandleAsync)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private sealed class Handler(EmailSettingsScope scope, Func<OrganizationScope, Guid> scopeIdSelector)
    {
        public async ValueTask<Ok<IReadOnlyList<CustomBulkTemplateListItemDto>>> HandleAsync(
            string teamSlug,
            string? eventSlug,
            IOrganizationScopeResolver scopeResolver,
            IMediator mediator,
            CancellationToken ct)
        {
            var orgScope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, ct);
            var scopeId = scopeIdSelector(orgScope);

            var rows = await mediator.QueryAsync<GetCustomBulkTemplatesQuery, IReadOnlyList<CustomBulkTemplateListItemDto>>(
                new GetCustomBulkTemplatesQuery(scope, scopeId), ct);

            return TypedResults.Ok(rows);
        }
    }
}
