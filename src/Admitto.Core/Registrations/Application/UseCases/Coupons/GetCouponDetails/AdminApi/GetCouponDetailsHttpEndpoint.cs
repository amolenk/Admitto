using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Coupons.GetCouponDetails.AdminApi;

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
        IQueryHandler<GetCouponDetailsQuery, CouponDetailsDto> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetCouponDetailsQuery(
            TicketedEventId.From(eventId),
            TeamId.From(teamId),
            CouponId.From(couponId));

        var result = await handler.HandleAsync(query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
