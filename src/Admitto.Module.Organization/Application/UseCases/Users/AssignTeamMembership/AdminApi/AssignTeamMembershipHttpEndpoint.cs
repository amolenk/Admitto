using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.Users.AssignTeamMembership.AdminApi;

public static class AssignTeamMembershipHttpEndpoint
{
    public static RouteGroupBuilder MapAssignTeamMembership(this RouteGroupBuilder group)
    {
        group
            .MapPost("/members", AssignTeamMembership)
            .WithName(nameof(AssignTeamMembership))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok> AssignTeamMembership(
        OrganizationScope organizationScope,
        AssignTeamMembershipHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(OrganizationModuleKey.Value)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(organizationScope.TeamId);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}