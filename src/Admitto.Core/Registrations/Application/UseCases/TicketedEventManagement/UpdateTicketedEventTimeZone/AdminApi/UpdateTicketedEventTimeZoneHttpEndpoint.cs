using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventTimeZone.AdminApi;

public static class UpdateTicketedEventTimeZoneHttpEndpoint
{
    public static RouteGroupBuilder MapUpdateTicketedEventTimeZone(this RouteGroupBuilder group)
    {
        group
            .MapPut("/time-zone", UpdateTicketedEventTimeZone)
            .WithName(nameof(UpdateTicketedEventTimeZone))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> UpdateTicketedEventTimeZone(
        Guid teamId,
        Guid eventId,
        UpdateTicketedEventTimeZoneHttpRequest request,
        ICommandHandler<UpdateTicketedEventTimeZoneCommand> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(eventId);

        await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
