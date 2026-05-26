using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.UpdateTicketType.AdminApi;

public static class UpdateTicketTypeHttpEndpoint
{
    public static RouteGroupBuilder MapUpdateTicketType(this RouteGroupBuilder group)
    {
        group
            .MapPut("/{ticketTypeId:guid}", UpdateTicketType)
            .WithName(nameof(UpdateTicketType))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> UpdateTicketType(
        Guid ticketTypeId,
        Guid teamId,
        Guid eventId,
        UpdateTicketTypeHttpRequest request,
        ICommandHandler<UpdateTicketTypeCommand> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(
            eventId,
            ticketTypeId);

        await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
