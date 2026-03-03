using Amolenk.Admitto.Shared.Application.Auth;
using Amolenk.Admitto.Shared.Application.Http;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Application.Persistence;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.Users.AssignTeamMembership.AdminApi;

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