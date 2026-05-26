using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CancelRegistration.AdminApi;

public static class CancelRegistrationHttpEndpoint
{
    public static RouteGroupBuilder MapCancelRegistration(this RouteGroupBuilder group)
    {
        group
            .MapPost("/registrations/{registrationId:guid}/cancel", CancelRegistration)
            .WithName(nameof(CancelRegistration))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> CancelRegistration(
        Guid registrationId,
        Guid teamId,
        Guid eventId,
        CancelRegistrationHttpRequest request,
        ICommandHandler<CancelRegistrationCommand> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var reason = Enum.Parse<CancellationReason>(request.Reason!);

        var command = new CancelRegistrationCommand(
            registrationId,
            eventId,
            reason);

        await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
