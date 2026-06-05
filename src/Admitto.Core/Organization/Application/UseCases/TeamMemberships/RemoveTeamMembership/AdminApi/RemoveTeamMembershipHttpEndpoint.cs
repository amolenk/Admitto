using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.RemoveTeamMembership.AdminApi;

public static class RemoveTeamMembershipHttpEndpoint
{
    public static RouteGroupBuilder MapRemoveTeamMembership(this RouteGroupBuilder group)
    {
        group
            .MapDelete("/{email}", RemoveTeamMembership)
            .WithName(nameof(RemoveTeamMembership))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Owner));

        return group;
    }

    private static async ValueTask<Ok> RemoveTeamMembership(
        Guid teamId,
        string email,
        ICommandHandler<RemoveTeamMembershipCommand> handler,
        [FromKeyedServices(OrganizationModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new RemoveTeamMembershipCommand(teamId, email);

        await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
