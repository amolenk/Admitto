using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.TicketedEventManagement.CancelTicketedEvent.AdminApi;

public static class CancelTicketedEventHttpEndpoint
{
    public static RouteGroupBuilder MapCancelTicketedEvent(this RouteGroupBuilder group)
    {
        group
            .MapPost("/cancel", CancelTicketedEvent)
            .WithName(nameof(CancelTicketedEvent))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> CancelTicketedEvent(
        Guid teamId,
        Guid eventId,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new CancelTicketedEventCommand(eventId);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
