using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.RevokeCoupon.AdminApi;

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
        OrganizationScope organizationScope,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new RevokeCouponCommand(
            TicketedEventId.From(organizationScope.EventId!.Value),
            CouponId.From(couponId));

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
