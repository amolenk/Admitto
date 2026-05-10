using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.CreateTeam.AdminApi;

public static class CreateTeamHttpEndpoint
{
    public static RouteGroupBuilder MapCreateTeam(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", CreateTeam)
            .WithName(nameof(CreateTeam))
            .RequireAuthorization(policy => policy.RequireAdminRole());

        return group;
    }

    private static async ValueTask<Ok> CreateTeam(
        CreateTeamHttpRequest request,
        CreateTeamHandler handler,
        [FromKeyedServices(OrganizationModuleKey.Value)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand();

        await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}