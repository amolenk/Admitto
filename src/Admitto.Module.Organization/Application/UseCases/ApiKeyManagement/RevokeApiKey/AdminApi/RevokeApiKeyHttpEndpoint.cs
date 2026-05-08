using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.ApiKeyManagement.RevokeApiKey.AdminApi;

public static class RevokeApiKeyHttpEndpoint
{
    public static RouteGroupBuilder MapRevokeApiKey(this RouteGroupBuilder group)
    {
        group
            .MapDelete("/{keyId:guid}", RevokeApiKey)
            .WithName(nameof(RevokeApiKey))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Owner));

        return group;
    }

    private static async ValueTask<NoContent> RevokeApiKey(
        Guid teamId,
        Guid keyId,
        IMediator mediator,
        [FromKeyedServices(OrganizationModuleKey.Value)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new RevokeApiKeyCommand(teamId, keyId);

        await mediator.SendAsync(command, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
