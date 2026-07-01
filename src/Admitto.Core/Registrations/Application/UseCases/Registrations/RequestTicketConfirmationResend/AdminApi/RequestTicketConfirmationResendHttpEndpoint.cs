using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RequestTicketConfirmationResend.AdminApi;

public static class RequestTicketConfirmationResendHttpEndpoint
{
    public static RouteGroupBuilder MapRequestTicketConfirmationResend(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{registrationId:guid}/ticket-email/resend", RequestTicketConfirmationResend)
            .WithName(nameof(RequestTicketConfirmationResend))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Accepted> RequestTicketConfirmationResend(
        Guid teamId,
        Guid eventId,
        Guid registrationId,
        ICommandHandler<RequestTicketConfirmationResendCommand> commandHandler,
        [FromKeyedServices(RegistrationsModule.Key)] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        await commandHandler.HandleAsync(
            new RequestTicketConfirmationResendCommand(
                TeamId: teamId,
                TicketedEventId: eventId,
                RegistrationId: registrationId,
                ResendRequestId: Guid.NewGuid()),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Accepted((string?)null);
    }
}
