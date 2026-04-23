using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

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
        string teamSlug,
        string eventSlug,
        RegisterWithCouponHttpRequest request,
        IOrganizationFacade facade,
        ITicketedEventIdLookup ticketedEventIdLookup,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var teamId = await facade.GetTeamIdAsync(teamSlug, cancellationToken);
        var eventId = await ticketedEventIdLookup.GetTicketedEventIdAsync(teamId, eventSlug, cancellationToken);

        var command = new RegisterAttendeeCommand(
            TicketedEventId.From(eventId),
            EmailAddress.From(request.Email),
            request.TicketTypeSlugs,
            RegistrationMode.Coupon,
            CouponCode: request.CouponCode,
            EmailVerificationToken: null,
            AdditionalDetails: request.AdditionalDetails);

        var registrationId = await mediator.SendReceiveAsync<RegisterAttendeeCommand, RegistrationId>(
            command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId.Value}",
            null);
    }
}
