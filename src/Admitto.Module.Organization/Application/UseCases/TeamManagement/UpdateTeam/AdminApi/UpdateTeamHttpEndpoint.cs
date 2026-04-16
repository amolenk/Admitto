using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.UpdateTeam.AdminApi;

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
        string teamSlug,
        IOrganizationScopeResolver scopeResolver,
        UpdateTeamHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(OrganizationModuleKey.Value)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, cancellationToken: cancellationToken);

        var command = request.ToCommand(scope.TeamId);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}