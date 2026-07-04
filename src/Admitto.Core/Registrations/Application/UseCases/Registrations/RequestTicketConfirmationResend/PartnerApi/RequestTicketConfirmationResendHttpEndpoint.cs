using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ResolvePartnerTicketedEvent.PartnerApi;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RequestTicketConfirmationResend.PartnerApi;

public static class RequestTicketConfirmationResendHttpEndpoint
{
    public static RouteGroupBuilder MapPartnerRequestTicketConfirmationResend(this RouteGroupBuilder group)
    {
        group
            .MapPost("/registrations/{registrationId:guid}/ticket-email/resend", RequestTicketConfirmationResend)
            .WithName("PartnerRequestTicketConfirmationResend");

        return group;
    }

    private static async ValueTask<Accepted> RequestTicketConfirmationResend(
        HttpContext httpContext,
        string eventSlug,
        Guid registrationId,
        PartnerTicketedEventResolver eventResolver,
        ICommandHandler<RequestTicketConfirmationResendCommand> commandHandler,
        [FromKeyedServices(RegistrationsModule.Key)] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var teamId = httpContext.User.GetRequiredTeamId();
        var eventId = await eventResolver.ResolveAsync(TeamId.From(teamId), eventSlug, cancellationToken);

        await commandHandler.HandleAsync(
            new RequestTicketConfirmationResendCommand(
                TeamId: teamId,
                TicketedEventId: eventId.Value,
                RegistrationId: registrationId,
                ResendRequestId: Guid.NewGuid()),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Accepted((string?)null);
    }
}
