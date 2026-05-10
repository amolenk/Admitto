using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMembershipManagement.AssignTeamMembership.AdminApi;

public static class AssignTeamMembershipHttpEndpoint
{
    public static RouteGroupBuilder MapAssignTeamMembership(this RouteGroupBuilder group)
    {
        group
            .MapPost("/members", AssignTeamMembership)
            .WithName(nameof(AssignTeamMembership))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Owner));

        return group;
    }

    private static async ValueTask<Ok> AssignTeamMembership(
        Guid teamId,
        AssignTeamMembershipHttpRequest request,
        AssignTeamMembershipHandler handler,
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