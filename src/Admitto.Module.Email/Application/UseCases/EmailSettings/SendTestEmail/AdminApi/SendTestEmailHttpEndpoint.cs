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
        EmailSettingsScope scope)
    {
        var endpointName = scope == EmailSettingsScope.Team ? "TestTeamEmailSettings" : "TestEventEmailSettings";
        var handler = new Handler(scope);

        group
            .MapPost("/test", handler.HandleAsync)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private sealed class Handler(EmailSettingsScope scope)
    {
        public async ValueTask<Ok> HandleAsync(
            Guid teamId,
            Guid? eventId,
            SendTestEmailHttpRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var scopeId = scope == EmailSettingsScope.Event ? eventId!.Value : teamId;

            await mediator.SendAsync(request.ToCommand(scope, scopeId), ct);

            return TypedResults.Ok();
        }
    }
}
