using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets.AdminApi;

public static class ChangeAttendeeTicketsHttpEndpoint
{
    public static RouteGroupBuilder MapChangeAttendeeTickets(this RouteGroupBuilder group)
    {
        group
            .MapPut("/registrations/{registrationId:guid}/tickets", ChangeAttendeeTickets)
            .WithName(nameof(ChangeAttendeeTickets))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> ChangeAttendeeTickets(
        Guid registrationId,
        Guid teamId,
        Guid eventId,
        ChangeAttendeeTicketsHttpRequest request,
        ChangeAttendeeTicketsHandler handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new ChangeAttendeeTicketsCommand(
            eventId,
            registrationId,
            request.TicketTypeSlugs!,
            ChangeMode.Admin);

        await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
