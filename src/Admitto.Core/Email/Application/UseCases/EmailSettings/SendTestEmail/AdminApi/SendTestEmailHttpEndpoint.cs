using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Http;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.SendTestEmail.AdminApi;

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
