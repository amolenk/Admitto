using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.AssignTeamMembership.AdminApi;

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
        string teamSlug,
        IOrganizationScopeResolver scopeResolver,
        AssignTeamMembershipHttpRequest request,
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