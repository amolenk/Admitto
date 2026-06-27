using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeWithCoupon.PartnerApi;

public static class RegisterAttendeeWithCouponHttpEndpoint
{
    public static RouteGroupBuilder MapRegisterAttendeeWithCoupon(this RouteGroupBuilder group)
    {
        group.MapPost("/registrations/coupon", RegisterAttendeeWithCoupon)
            .WithName(nameof(RegisterAttendeeWithCoupon));

        return group;
    }

    private static async ValueTask<IResult> RegisterAttendeeWithCoupon(
        HttpContext httpContext,
        Guid eventId,
        RegisterAttendeeWithCouponHttpRequest request,
        ICommandHandler<RegisterAttendeeWithCouponCommand, Guid> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var teamId = httpContext.User.GetRequiredTeamId();
        var command = new RegisterAttendeeWithCouponCommand(
            eventId,
            teamId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.TicketTypeIds,
            request.CouponCode,
            AdditionalDetails: request.AdditionalDetails);

        var registrationId = await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/events/{eventId}/registrations/{registrationId}",
            null);
    }
}
