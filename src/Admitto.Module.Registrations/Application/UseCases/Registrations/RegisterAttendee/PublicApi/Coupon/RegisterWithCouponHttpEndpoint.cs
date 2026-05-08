using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterAttendee.PublicApi.Coupon;

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
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new RegisterAttendeeCommand(
            eventId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.TicketTypeSlugs,
            RegistrationMode.Coupon,
            CouponCode: request.CouponCode,
            EmailVerificationToken: null,
            AdditionalDetails: request.AdditionalDetails);

        var registrationId = await mediator.SendReceiveAsync<RegisterAttendeeCommand, Guid>(
            command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/teams/{teamId}/events/{eventId}/registrations/{registrationId}",
            null);
    }
}
