using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.TicketTypeManagement.CancelTicketType.AdminApi;

public static class CancelTicketTypeHttpEndpoint
{
    public static RouteGroupBuilder MapCancelTicketType(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{ticketTypeSlug}/cancel", CancelTicketType)
            .WithName(nameof(CancelTicketType))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> CancelTicketType(
        string ticketTypeSlug,
        Guid teamId,
        Guid eventId,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new CancelTicketTypeCommand(
            eventId,
            ticketTypeSlug);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
