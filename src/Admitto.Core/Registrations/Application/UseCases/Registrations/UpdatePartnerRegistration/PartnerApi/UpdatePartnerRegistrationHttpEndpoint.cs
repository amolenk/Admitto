using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ResolvePartnerTicketedEvent.PartnerApi;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.UpdatePartnerRegistration.PartnerApi;

public static class UpdatePartnerRegistrationHttpEndpoint
{
    public static RouteGroupBuilder MapUpdatePartnerRegistration(this RouteGroupBuilder group)
    {
        group.MapPut("/registrations/{registrationId:guid}", UpdatePartnerRegistration)
            .WithName(nameof(UpdatePartnerRegistration));

        return group;
    }

    private static async ValueTask<IResult> UpdatePartnerRegistration(
        HttpContext httpContext,
        string eventSlug,
        Guid registrationId,
        UpdatePartnerRegistrationHttpRequest request,
        PartnerTicketedEventResolver eventResolver,
        ICommandHandler<UpdatePartnerRegistrationCommand> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var teamId = httpContext.User.GetRequiredTeamId();
        var eventId = await eventResolver.ResolveAsync(TeamId.From(teamId), eventSlug, cancellationToken);
        var command = new UpdatePartnerRegistrationCommand(
            eventId.Value,
            teamId,
            registrationId,
            request.FirstName,
            request.LastName,
            request.TicketTypeIds ?? [],
            request.AdditionalDetails,
            request.WaitlistCouponCode);

        await handler.HandleAsync(command, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Ok();
    }
}
