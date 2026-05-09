using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.CouponManagement.RevokeCoupon.AdminApi;

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
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new RevokeCouponCommand(
            eventId,
            couponId);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
