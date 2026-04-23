using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.DeleteEmailTemplate.AdminApi;

public static class DeleteEmailTemplateHttpEndpoint
{
    public static RouteGroupBuilder MapDeleteEmailTemplate(
        this RouteGroupBuilder group,
        EmailSettingsScope scope,
        Func<OrganizationScope, Guid> scopeIdSelector)
    {
        var endpointName = scope == EmailSettingsScope.Team ? "DeleteTeamEmailTemplate" : "DeleteEventEmailTemplate";

        group
            .MapDelete("/", async (
                string teamSlug,
                string? eventSlug,
                string type,
                [FromQuery] uint version,
                IOrganizationScopeResolver scopeResolver,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                var unitOfWork = httpContext.RequestServices
                    .GetRequiredKeyedService<IUnitOfWork>(EmailModuleKey.Value);

                var orgScope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, ct);
                var scopeId = scopeIdSelector(orgScope);

                await mediator.SendAsync(new DeleteEmailTemplateCommand(scope, scopeId, type, version), ct);
                await unitOfWork.SaveChangesAsync(ct);

                return TypedResults.NoContent();
            })
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }
}
