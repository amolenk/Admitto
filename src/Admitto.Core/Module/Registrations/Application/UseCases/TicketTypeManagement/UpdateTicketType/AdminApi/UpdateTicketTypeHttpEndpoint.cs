using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.TicketTypeManagement.UpdateTicketType.AdminApi;

public static class UpdateTicketTypeHttpEndpoint
{
    public static RouteGroupBuilder MapUpdateTicketType(this RouteGroupBuilder group)
    {
        group
            .MapPut("/{ticketTypeSlug}", UpdateTicketType)
            .WithName(nameof(UpdateTicketType))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> UpdateTicketType(
        string ticketTypeSlug,
        Guid teamId,
        Guid eventId,
        UpdateTicketTypeHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(
            eventId,
            ticketTypeSlug);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
