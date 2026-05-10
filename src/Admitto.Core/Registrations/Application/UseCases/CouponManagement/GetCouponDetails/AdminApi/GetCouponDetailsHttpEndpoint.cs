using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.CouponManagement.GetCouponDetails.AdminApi;

public static class GetCouponDetailsHttpEndpoint
{
    public static RouteGroupBuilder MapGetCouponDetails(this RouteGroupBuilder group)
    {
        group
            .MapGet("/coupons/{couponId:guid}", GetCouponDetails)
            .WithName(nameof(GetCouponDetails))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok<CouponDetailsDto>> GetCouponDetails(
        Guid couponId,
        Guid teamId,
        Guid eventId,
        GetCouponDetailsHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetCouponDetailsQuery(
            TicketedEventId.From(eventId),
            CouponId.From(couponId));

        var result = await handler.HandleAsync(query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
