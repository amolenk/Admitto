using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.SendTestEmail.AdminApi;

public static class SendTestEmailHttpEndpoint
{
    public static RouteGroupBuilder MapSendTestEmail(
        this RouteGroupBuilder group,
        EmailSettingsScope scope,
        Func<OrganizationScope, Guid> scopeIdSelector)
    {
        var endpointName = scope == EmailSettingsScope.Team ? "TestTeamEmailSettings" : "TestEventEmailSettings";
        var handler = new Handler(scope, scopeIdSelector);

        group
            .MapPost("/test", handler.HandleAsync)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private sealed class Handler(EmailSettingsScope scope, Func<OrganizationScope, Guid> scopeIdSelector)
    {
        public async ValueTask<Ok> HandleAsync(
            string teamSlug,
            string? eventSlug,
            IOrganizationScopeResolver scopeResolver,
            SendTestEmailHttpRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var orgScope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, ct);
            var scopeId = scopeIdSelector(orgScope);

            await mediator.SendAsync(request.ToCommand(scope, scopeId), ct);

            return TypedResults.Ok();
        }
    }
}
