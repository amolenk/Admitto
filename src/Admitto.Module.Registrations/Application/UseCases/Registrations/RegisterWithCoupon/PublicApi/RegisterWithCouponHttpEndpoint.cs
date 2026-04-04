using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterWithCoupon.PublicApi;

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
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var teamId = await facade.GetTeamIdAsync(teamSlug, cancellationToken);
        var eventId = await facade.GetTicketedEventIdAsync(teamId, eventSlug, cancellationToken);

        var command = new RegisterWithCouponCommand(
            TicketedEventId.From(eventId),
            request.CouponCode,
            EmailAddress.From(request.Email),
            request.TicketTypeSlugs);

        var registrationId = await mediator.SendReceiveAsync<RegisterWithCouponCommand, RegistrationId>(
            command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId.Value}",
            null);
    }
}
