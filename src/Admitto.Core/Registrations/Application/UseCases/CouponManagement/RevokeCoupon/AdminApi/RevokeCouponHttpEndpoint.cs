using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.CouponManagement.RevokeCoupon.AdminApi;

public static class RevokeCouponHttpEndpoint
{
    public static RouteGroupBuilder MapRevokeCoupon(this RouteGroupBuilder group)
    {
        group
            .MapPost("/coupons/{couponId:guid}/revoke", RevokeCoupon)
            .WithName(nameof(RevokeCoupon))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok> RevokeCoupon(
        Guid couponId,
        Guid teamId,
        Guid eventId,
        ICommandHandler<RevokeCouponCommand> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new RevokeCouponCommand(
            eventId,
            couponId);

        await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
