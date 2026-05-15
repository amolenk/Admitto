using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendee.PublicApi.Coupon;

public static class RegisterWithCouponHttpEndpoint
{
    public static RouteGroupBuilder MapRegisterWithCoupon(this RouteGroupBuilder group)
    {
        group.MapPost("/registrations/coupon", HandleAsync)
            .WithName(nameof(RegisterWithCouponHttpEndpoint));

        return group;
    }

    private static async ValueTask<IResult> HandleAsync(
        Guid teamId,
        Guid eventId,
        RegisterWithCouponHttpRequest request,
        RegisterAttendeeHandler handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new RegisterAttendeeCommand(
            eventId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.TicketTypeIds,
            RegistrationMode.Coupon,
            CouponCode: request.CouponCode,
            EmailVerificationToken: null,
            AdditionalDetails: request.AdditionalDetails);

        var registrationId = await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/teams/{teamId}/events/{eventId}/registrations/{registrationId}",
            null);
    }
}
