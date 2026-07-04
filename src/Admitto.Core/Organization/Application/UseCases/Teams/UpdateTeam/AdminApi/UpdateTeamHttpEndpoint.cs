using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.UpdateTeam.AdminApi;

public static class UpdateTeamHttpEndpoint
{
    public static RouteGroupBuilder MapUpdateTeam(this RouteGroupBuilder group)
    {
        group
            .MapPut("/", UpdateTeam)
            .WithName(nameof(UpdateTeam))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Owner));

        return group;
    }

    private static async ValueTask<Ok> UpdateTeam(
        Guid teamId,
        UpdateTeamHttpRequest request,
        ICommandHandler<UpdateTeamCommand> handler,
        [FromKeyedServices(OrganizationModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(teamId);

        await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
