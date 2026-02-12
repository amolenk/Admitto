using Amolenk.Admitto.Shared.Application.Auth;
using Amolenk.Admitto.Shared.Application.Persistence;

namespace Amolenk.Admitto.Organization.Application.UseCases.Teams.CreateTeam.AdminApi;

public static class CreateTeamHttpEndpoint
{
    public static RouteGroupBuilder MapCreateTeam(this RouteGroupBuilder group)
    {
        group
            .MapPost("/teams", CreateTeam)
            .WithName(nameof(CreateTeam))
            .RequireAuthorization(policy => policy.RequireAdminRole());

        return group;
    }

    private static async ValueTask<Ok> CreateTeam(
        CreateTeamHttpRequest request,
        [FromKeyedServices(OrganizationModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        await unitOfWork.RunAsync(
            (mediator, ct) =>
            {
                var command = request.ToCommand();
                
                return mediator.SendAsync(command, ct);
            },
            cancellationToken);

        return TypedResults.Ok();
    }
}