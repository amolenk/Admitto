using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.GetEmailTemplate.AdminApi;

public static class GetEmailTemplateHttpEndpoint
{
    public static RouteGroupBuilder MapGetEmailTemplate(
        this RouteGroupBuilder group,
        EmailSettingsScope scope,
        Func<OrganizationScope, Guid> scopeIdSelector)
    {
        var endpointName = scope == EmailSettingsScope.Team ? "GetTeamEmailTemplate" : "GetEventEmailTemplate";

        group
            .MapGet("/", async (
                string teamSlug,
                string? eventSlug,
                string type,
                IOrganizationScopeResolver scopeResolver,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var orgScope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, ct);
                var scopeId = scopeIdSelector(orgScope);

                var dto = await mediator.QueryAsync<GetEmailTemplateQuery, EmailTemplateDto?>(
                    new GetEmailTemplateQuery(scope, scopeId, type), ct);

                if (dto is null)
                    throw new BusinessRuleViolationException(
                        NotFoundError.Create<Domain.Entities.EmailTemplate>(type));

                return TypedResults.Ok(dto);
            })
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }
}
