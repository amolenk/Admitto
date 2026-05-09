using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMembershipManagement.ChangeTeamMembershipRole.AdminApi;

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
        IMediator mediator,
        [FromKeyedServices(OrganizationModuleKey.Value)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(teamId, email);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
