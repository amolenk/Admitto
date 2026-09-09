using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ReconfirmRegistration.AdminApi;

public static class ReconfirmRegistrationHttpEndpoint
{
    public static RouteGroupBuilder MapAdminReconfirmRegistration(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{registrationId:guid}/reconfirm", AdminReconfirmRegistration)
            .WithName(nameof(AdminReconfirmRegistration))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> AdminReconfirmRegistration(
        Guid registrationId,
        Guid teamId,
        Guid eventId,
        ICommandHandler<ReconfirmRegistrationCommand> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new ReconfirmRegistrationCommand(
            registrationId,
            eventId,
            teamId);

        await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
