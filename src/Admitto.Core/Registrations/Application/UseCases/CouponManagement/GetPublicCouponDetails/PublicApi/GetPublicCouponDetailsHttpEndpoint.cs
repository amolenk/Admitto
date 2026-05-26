using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.CouponManagement.GetPublicCouponDetails.PublicApi;

public static class GetPublicCouponDetailsHttpEndpoint
{
    public static RouteGroupBuilder MapGetPublicCouponDetails(this RouteGroupBuilder group)
    {
        group
            .MapGet("/coupons/{couponCode:guid}", GetPublicCouponDetails)
            .WithName(nameof(GetPublicCouponDetails))
            .AllowAnonymous();

        return group;
    }

    private static async ValueTask<Ok<PublicCouponDetailsDto>> GetPublicCouponDetails(
        Guid eventId,
        Guid couponCode,
        IQueryHandler<GetPublicCouponDetailsQuery, PublicCouponDetailsDto> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetPublicCouponDetailsQuery(
            TicketedEventId.From(eventId),
            CouponCode.From(couponCode));

        var result = await handler.HandleAsync(query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
