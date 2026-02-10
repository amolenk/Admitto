using Amolenk.Admitto.Shared.Application.Auth;
using Amolenk.Admitto.Shared.Application.Http;
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
        [FromKeyedServices(OrganizationModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        await unitOfWork.RunAsync(
            (mediator, ct) =>
            {
                var command = request.ToCommand(organizationScope.TeamId);
                
                return mediator.SendAsync(command, ct);
            },
            cancellationToken);

        return TypedResults.Ok();
    }
}