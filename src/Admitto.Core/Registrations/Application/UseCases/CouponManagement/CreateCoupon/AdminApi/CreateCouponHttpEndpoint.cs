using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.CouponManagement.CreateCoupon.AdminApi;

public static class CreateCouponHttpEndpoint
{
    public static RouteGroupBuilder MapCreateCoupon(this RouteGroupBuilder group)
    {
        group
            .MapPost("/coupons", CreateCoupon)
            .WithName(nameof(CreateCoupon))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Created<CreateCouponHttpResponse>> CreateCoupon(
        Guid teamId,
        Guid eventId,
        CreateCouponHttpRequest request,
        ICommandHandler<CreateCouponCommand, Guid> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(teamId, eventId);

        var couponId = await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/teams/{teamId}/events/{eventId}/coupons/{couponId}",
            new CreateCouponHttpResponse(couponId));
    }
}
