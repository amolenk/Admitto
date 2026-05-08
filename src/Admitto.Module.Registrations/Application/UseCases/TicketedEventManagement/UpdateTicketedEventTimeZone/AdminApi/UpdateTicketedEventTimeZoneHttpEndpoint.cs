using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventTimeZone.AdminApi;

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
