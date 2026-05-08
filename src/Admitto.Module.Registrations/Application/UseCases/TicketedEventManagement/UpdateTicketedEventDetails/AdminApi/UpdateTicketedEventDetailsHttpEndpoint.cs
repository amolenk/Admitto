using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventDetails.AdminApi;

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
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(eventId);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
