using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.CreateCoupon.AdminApi;

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
        string teamSlug,
        string eventSlug,
        IOrganizationScopeResolver scopeResolver,
        CreateCouponHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, cancellationToken);

        var command = request.ToCommand(scope.EventId!.Value);

        var couponId = await mediator.SendReceiveAsync<CreateCouponCommand, Guid>(
            command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/teams/{teamSlug}/events/{eventSlug}/coupons/{couponId}",
            new CreateCouponHttpResponse(couponId));
    }
}
