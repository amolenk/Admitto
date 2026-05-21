using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.CouponManagement.ListCoupons.AdminApi;

public static class ListCouponsHttpEndpoint
{
    public static RouteGroupBuilder MapListCoupons(this RouteGroupBuilder group)
    {
        group
            .MapGet("/coupons", ListCoupons)
            .WithName(nameof(ListCoupons))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok<ListCouponsResult>> ListCoupons(
        Guid teamId,
        Guid eventId,
        IQueryHandler<ListCouponsQuery, ListCouponsResult> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListCouponsQuery(TicketedEventId.From(eventId)), cancellationToken);

        return TypedResults.Ok(result);
    }
}
