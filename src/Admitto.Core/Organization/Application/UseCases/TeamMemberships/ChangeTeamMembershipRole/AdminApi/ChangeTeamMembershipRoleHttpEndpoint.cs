using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.ChangeTeamMembershipRole.AdminApi;

public static class ChangeTeamMembershipRoleHttpEndpoint
{
    public static RouteGroupBuilder MapChangeTeamMembershipRole(this RouteGroupBuilder group)
    {
        group
            .MapPut("/{email}", ChangeTeamMembershipRole)
            .WithName(nameof(ChangeTeamMembershipRole))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Owner));

        return group;
    }

    private static async ValueTask<Ok> ChangeTeamMembershipRole(
        Guid teamId,
        string email,
        ChangeTeamMembershipRoleHttpRequest request,
        ICommandHandler<ChangeTeamMembershipRoleCommand> handler,
        [FromKeyedServices(OrganizationModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(teamId, email);

        await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
