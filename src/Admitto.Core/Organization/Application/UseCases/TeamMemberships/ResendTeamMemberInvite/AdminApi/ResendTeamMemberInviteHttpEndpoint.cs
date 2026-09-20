using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.ResendTeamMemberInvite.AdminApi;

public static class ResendTeamMemberInviteHttpEndpoint
{
    public static RouteGroupBuilder MapResendTeamMemberInvite(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{email}/resend-invite", ResendTeamMemberInvite)
            .WithName(nameof(ResendTeamMemberInvite))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Owner));

        return group;
    }

    private static async ValueTask<Ok> ResendTeamMemberInvite(
        Guid teamId,
        string email,
        ICommandHandler<ResendTeamMemberInviteCommand> handler,
        [FromKeyedServices(OrganizationModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new ResendTeamMemberInviteCommand(teamId, email);

        await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
