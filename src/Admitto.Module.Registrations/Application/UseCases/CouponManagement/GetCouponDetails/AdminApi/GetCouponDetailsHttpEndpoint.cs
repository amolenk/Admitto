using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.GetCouponDetails.AdminApi;

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
        OrganizationScope organizationScope,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetCouponDetailsQuery(
            TicketedEventId.From(organizationScope.EventId!.Value),
            CouponId.From(couponId));

        var result = await mediator.QueryAsync<GetCouponDetailsQuery, CouponDetailsDto>(
            query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
