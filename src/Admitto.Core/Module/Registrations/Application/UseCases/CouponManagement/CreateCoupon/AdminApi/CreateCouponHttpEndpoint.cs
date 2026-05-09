using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.CouponManagement.CreateCoupon.AdminApi;

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
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(eventId);

        var couponId = await mediator.SendReceiveAsync<CreateCouponCommand, Guid>(
            command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/teams/{teamId}/events/{eventId}/coupons/{couponId}",
            new CreateCouponHttpResponse(couponId));
    }
}
