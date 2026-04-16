using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.ChangeTeamMembershipRole.AdminApi;

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
        string teamSlug,
        IOrganizationScopeResolver scopeResolver,
        string email,
        ChangeTeamMembershipRoleHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(OrganizationModuleKey.Value)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, cancellationToken: cancellationToken);

        var command = request.ToCommand(scope.TeamId, email);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
