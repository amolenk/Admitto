using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.SendTestEmail.AdminApi;

public static class SendTestEmailHttpEndpoint
{
    public static RouteGroupBuilder MapSendTestEmail(
        this RouteGroupBuilder group,
        bool isEventScoped)
    {
        var endpointName = isEventScoped ? "TestEventEmailSettings" : "TestTeamEmailSettings";
        var handler = new Handler(isEventScoped);

        group
            .MapPost("/test", handler.HandleAsync)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private sealed class Handler(bool isEventScoped)
    {
        public async ValueTask<Ok> HandleAsync(
            Guid teamId,
            Guid? eventId,
            SendTestEmailHttpRequest request,
            ICommandHandler<SendTestEmailCommand> handler,
            CancellationToken ct)
        {
            var ticketedEventId = isEventScoped ? eventId!.Value : (Guid?)null;

            await handler.HandleAsync(request.ToCommand(teamId, ticketedEventId), ct);

            return TypedResults.Ok();
        }
    }
}
