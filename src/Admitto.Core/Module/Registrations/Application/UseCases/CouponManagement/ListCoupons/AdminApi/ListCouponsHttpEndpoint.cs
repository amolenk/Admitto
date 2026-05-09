using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.CouponManagement.ListCoupons.AdminApi;

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
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new ListCouponsQuery(TicketedEventId.From(eventId));

        var result = await mediator.QueryAsync<ListCouponsQuery, ListCouponsResult>(
            query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
