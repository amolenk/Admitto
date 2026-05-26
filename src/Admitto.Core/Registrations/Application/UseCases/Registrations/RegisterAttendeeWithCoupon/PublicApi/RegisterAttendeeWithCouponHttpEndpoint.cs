using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeWithCoupon.PublicApi;

public static class RegisterAttendeeWithCouponHttpEndpoint
{
    public static RouteGroupBuilder MapRegisterAttendeeWithCoupon(this RouteGroupBuilder group)
    {
        group.MapPost("/registrations/coupon", HandleAsync)
            .WithName(nameof(RegisterAttendeeWithCouponHttpEndpoint));

        return group;
    }

    private static async ValueTask<IResult> HandleAsync(
        Guid teamId,
        Guid eventId,
        RegisterAttendeeWithCouponHttpRequest request,
        ICommandHandler<RegisterAttendeeWithCouponCommand, Guid> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new RegisterAttendeeWithCouponCommand(
            eventId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.TicketTypeIds,
            request.CouponCode,
            AdditionalDetails: request.AdditionalDetails);

        var registrationId = await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/teams/{teamId}/events/{eventId}/registrations/{registrationId}",
            null);
    }
}
