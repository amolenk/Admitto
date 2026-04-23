using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.UpsertEmailSettings.AdminApi;

public static class UpsertEmailSettingsHttpEndpoint
{
    public static RouteGroupBuilder MapUpsertEmailSettings(
        this RouteGroupBuilder group,
        EmailSettingsScope scope,
        Func<OrganizationScope, Guid> scopeIdSelector)
    {
        var endpointName = scope == EmailSettingsScope.Team ? "UpsertTeamEmailSettings" : "UpsertEventEmailSettings";
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
            IOrganizationScopeResolver scopeResolver,
            UpsertEmailSettingsHttpRequest request,
            IMediator mediator,
            [FromKeyedServices(EmailModuleKey.Value)] IUnitOfWork unitOfWork,
            CancellationToken ct)
        {
            var orgScope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, ct);
            var scopeId = scopeIdSelector(orgScope);

            if (request.Version is { } expectedVersion)
            {
                await mediator.SendAsync(request.ToUpdateCommand(scope, scopeId, expectedVersion), ct);
                await unitOfWork.SaveChangesAsync(ct);
                return TypedResults.Ok();
            }

            await mediator.SendAsync(request.ToCreateCommand(scope, scopeId), ct);
            await unitOfWork.SaveChangesAsync(ct);

            var location = eventSlug is not null
                ? $"/admin/teams/{teamSlug}/events/{eventSlug}/email-settings"
                : $"/admin/teams/{teamSlug}/email-settings";

            return TypedResults.Created(location);
        }
    }
}
