using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ResolvePartnerTicketedEvent.PartnerApi;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Coupons.GetPublicCouponDetails.PartnerApi;

public static class GetPublicCouponDetailsHttpEndpoint
{
    public static RouteGroupBuilder MapGetPublicCouponDetails(this RouteGroupBuilder group)
    {
        group
            .MapGet("/coupons/{couponCode:guid}", GetPublicCouponDetails)
            .WithName(nameof(GetPublicCouponDetails));

        return group;
    }

    private static async ValueTask<Ok<PublicCouponDetailsDto>> GetPublicCouponDetails(
        HttpContext httpContext,
        string eventSlug,
        Guid couponCode,
        PartnerTicketedEventResolver eventResolver,
        IQueryHandler<GetPublicCouponDetailsQuery, PublicCouponDetailsDto> handler,
        CancellationToken cancellationToken)
    {
        var teamId = httpContext.User.GetRequiredTeamId();
        var eventId = await eventResolver.ResolveAsync(TeamId.From(teamId), eventSlug, cancellationToken);
        var query = new GetPublicCouponDetailsQuery(
            eventId,
            TeamId.From(teamId),
            CouponCode.From(couponCode));

        var result = await handler.HandleAsync(query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
