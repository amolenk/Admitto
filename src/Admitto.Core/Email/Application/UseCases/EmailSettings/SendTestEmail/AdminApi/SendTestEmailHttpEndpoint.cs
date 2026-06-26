using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.SendTestEmail.AdminApi;

public static class SendTestEmailHttpEndpoint
{
    public static RouteGroupBuilder MapSendTestEmail(
        this RouteGroupBuilder group)
    {
        group
            .MapPost("/test", HandleAsync)
            .WithName("TestTeamEmailSettings")
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok> HandleAsync(
        Guid teamId,
        SendTestEmailHttpRequest request,
        ICommandHandler<SendTestEmailCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(request.ToCommand(teamId), ct);

        return TypedResults.Ok();
    }
}
