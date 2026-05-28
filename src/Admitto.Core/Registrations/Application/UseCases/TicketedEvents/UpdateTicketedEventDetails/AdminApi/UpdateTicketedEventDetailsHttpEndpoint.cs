using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.UpdateTicketedEventDetails.AdminApi;

public static class UpdateTicketedEventDetailsHttpEndpoint
{
    public static RouteGroupBuilder MapUpdateTicketedEventDetails(this RouteGroupBuilder group)
    {
        group
            .MapPut("/", UpdateTicketedEventDetails)
            .WithName(nameof(UpdateTicketedEventDetails))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> UpdateTicketedEventDetails(
        Guid teamId,
        Guid eventId,
        UpdateTicketedEventDetailsHttpRequest request,
        ICommandHandler<UpdateTicketedEventDetailsCommand> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(eventId, teamId);

        await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
