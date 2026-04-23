using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.DeleteEmailSettings.AdminApi;

public static class DeleteEmailSettingsHttpEndpoint
{
    public static RouteGroupBuilder MapDeleteEmailSettings(
        this RouteGroupBuilder group,
        EmailSettingsScope scope,
        Func<OrganizationScope, Guid> scopeIdSelector)
    {
        var endpointName = scope == EmailSettingsScope.Team ? "DeleteTeamEmailSettings" : "DeleteEventEmailSettings";

        group
            .MapDelete("/", async (
                string teamSlug,
                string? eventSlug,
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

                await mediator.SendAsync(new DeleteEmailSettingsCommand(scope, scopeId, version), ct);
                await unitOfWork.SaveChangesAsync(ct);

                return TypedResults.NoContent();
            })
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }
}
