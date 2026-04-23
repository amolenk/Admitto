using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.UpsertEmailTemplate.AdminApi;

public static class UpsertEmailTemplateHttpEndpoint
{
    public static RouteGroupBuilder MapUpsertEmailTemplate(
        this RouteGroupBuilder group,
        EmailSettingsScope scope,
        Func<OrganizationScope, Guid> scopeIdSelector)
    {
        var endpointName = scope == EmailSettingsScope.Team ? "UpsertTeamEmailTemplate" : "UpsertEventEmailTemplate";
        var handler = new Handler(scope, scopeIdSelector);

        group
            .MapPut("/", handler.HandleAsync)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private sealed class Handler(EmailSettingsScope scope, Func<OrganizationScope, Guid> scopeIdSelector)
    {
        public async ValueTask<Results<Ok, Created>> HandleAsync(
            string teamSlug,
            string? eventSlug,
            string type,
            IOrganizationScopeResolver scopeResolver,
            UpsertEmailTemplateHttpRequest request,
            IMediator mediator,
            [FromKeyedServices(EmailModuleKey.Value)] IUnitOfWork unitOfWork,
            CancellationToken ct)
        {
            var orgScope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, ct);
            var scopeId = scopeIdSelector(orgScope);

            await mediator.SendAsync(request.ToCommand(scope, scopeId, type), ct);
            await unitOfWork.SaveChangesAsync(ct);

            if (request.Version is not null)
                return TypedResults.Ok();

            var location = eventSlug is not null
                ? $"/admin/teams/{teamSlug}/events/{eventSlug}/email-templates/{type}"
                : $"/admin/teams/{teamSlug}/email-templates/{type}";

            return TypedResults.Created(location);
        }
    }
}
