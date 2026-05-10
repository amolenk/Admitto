using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.UpdateTeam.AdminApi;

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
        UpdateTeamHandler handler,
        [FromKeyedServices(OrganizationModuleKey.Value)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(teamId);

        await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}