using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.RemoveTeamMembership.AdminApi;

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
        string teamSlug,
        IOrganizationScopeResolver scopeResolver,
        string email,
        IMediator mediator,
        [FromKeyedServices(OrganizationModuleKey.Value)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, cancellationToken: cancellationToken);

        var command = new RemoveTeamMembershipCommand(scope.TeamId, email);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
