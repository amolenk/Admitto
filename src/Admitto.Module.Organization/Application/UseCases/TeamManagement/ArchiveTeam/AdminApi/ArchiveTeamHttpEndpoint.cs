using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.ArchiveTeam.AdminApi;

/// <summary>
/// POST /admin/teams/{teamId}/archive — archives the team (requires Owner membership).
/// </summary>
public static class ArchiveTeamHttpEndpoint
{
    /// <summary>Maps the POST /{teamId}/archive endpoint onto the provided route group.</summary>
    public static RouteGroupBuilder MapArchiveTeam(this RouteGroupBuilder group)
    {
        group
            .MapPost("/archive", ArchiveTeam)
            .WithName(nameof(ArchiveTeam))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Owner));

        return group;
    }

    private static async ValueTask<Ok> ArchiveTeam(
        Guid teamId,
        ArchiveTeamHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(OrganizationModuleKey.Value)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(teamId);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
