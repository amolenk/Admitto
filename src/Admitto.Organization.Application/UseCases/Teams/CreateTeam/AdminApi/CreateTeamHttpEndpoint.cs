using Amolenk.Admitto.Shared.Application.Auth;
using Amolenk.Admitto.Shared.Application.Messaging;
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
        IMediator mediator,
        [FromKeyedServices(OrganizationModuleKey.Value)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand();

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}