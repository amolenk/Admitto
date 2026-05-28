using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.AddBadgeType.AdminApi;

public static class AddBadgeTypeHttpEndpoint
{
    public static RouteGroupBuilder MapAddBadgeType(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", AddBadgeType)
            .WithName(nameof(AddBadgeType))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Created<AddBadgeTypeHttpResponse>> AddBadgeType(
        Guid teamId,
        Guid eventId,
        AddBadgeTypeHttpRequest request,
        ICommandHandler<AddBadgeTypeCommand, Guid> handler,
        [FromKeyedServices(BadgesModule.Key)] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var id = await handler.HandleAsync(request.ToCommand(eventId, teamId), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/teams/{teamId}/events/{eventId}/badge-types/{id}",
            new AddBadgeTypeHttpResponse(id));
    }
}
