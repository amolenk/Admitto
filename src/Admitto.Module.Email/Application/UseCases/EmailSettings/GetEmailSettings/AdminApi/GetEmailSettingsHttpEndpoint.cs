using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.GetEmailSettings.AdminApi;

public static class GetEmailSettingsHttpEndpoint
{
    public static RouteGroupBuilder MapGetEmailSettings(
        this RouteGroupBuilder group,
        EmailSettingsScope scope,
        Func<OrganizationScope, Guid> scopeIdSelector)
    {
        var endpointName = scope == EmailSettingsScope.Team ? "GetTeamEmailSettings" : "GetEventEmailSettings";

        group
            .MapGet("/", async (
                string teamSlug,
                string? eventSlug,
                IOrganizationScopeResolver scopeResolver,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var orgScope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, ct);
                var scopeId = scopeIdSelector(orgScope);

                var dto = await mediator.QueryAsync<GetEmailSettingsQuery, EmailSettingsDto?>(
                    new GetEmailSettingsQuery(scope, scopeId), ct);

                if (dto is null)
                    throw new BusinessRuleViolationException(
                        NotFoundError.Create<Domain.Entities.EmailSettings>(teamSlug));

                return TypedResults.Ok(dto);
            })
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }
}
