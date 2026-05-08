using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.ApiKeyManagement.CreateApiKey.AdminApi;

public static class CreateApiKeyHttpEndpoint
{
    public static RouteGroupBuilder MapCreateApiKey(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", CreateApiKey)
            .WithName(nameof(CreateApiKey))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Owner));

        return group;
    }

    private static async ValueTask<Created<CreateApiKeyHttpResponse>> CreateApiKey(
        Guid teamId,
        IUserContextAccessor userContextAccessor,
        CreateApiKeyHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(OrganizationModuleKey.Value)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(teamId, userContextAccessor.Current.UserName);
        var result = await mediator.SendReceiveAsync<CreateApiKeyCommand, CreateApiKeyResult>(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new CreateApiKeyHttpResponse(result.KeyId, request.Name, result.KeyPrefix, result.RawKey);
        return TypedResults.Created($"/admin/teams/{teamId}/api-keys/{result.KeyId}", response);
    }
}
