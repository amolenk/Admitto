using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.TicketedEventManagement.ArchiveTicketedEvent.AdminApi;

public static class ArchiveTicketedEventHttpEndpoint
{
    public static RouteGroupBuilder MapArchiveTicketedEvent(this RouteGroupBuilder group)
    {
        group
            .MapPost("/archive", ArchiveTicketedEvent)
            .WithName(nameof(ArchiveTicketedEvent))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> ArchiveTicketedEvent(
        Guid teamId,
        Guid eventId,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new ArchiveTicketedEventCommand(eventId);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
