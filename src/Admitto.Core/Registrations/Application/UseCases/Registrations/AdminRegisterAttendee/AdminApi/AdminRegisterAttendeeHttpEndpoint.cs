using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.AdminRegisterAttendee.AdminApi;

public static class AdminRegisterAttendeeHttpEndpoint
{
    public static RouteGroupBuilder MapAdminRegisterAttendee(this RouteGroupBuilder group)
    {
        group
            .MapPost("/registrations", AdminRegisterAttendee)
            .WithName(nameof(AdminRegisterAttendee))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Created<AdminRegisterAttendeeHttpResponse>> AdminRegisterAttendee(
        Guid teamId,
        Guid eventId,
        AdminRegisterAttendeeHttpRequest request,
        ICommandHandler<AdminRegisterAttendeeCommand, Guid> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new AdminRegisterAttendeeCommand(
            eventId,
            teamId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.TicketTypeIds,
            AdditionalDetails: request.AdditionalDetails);

        var registrationId = await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/admin/teams/{teamId}/events/{eventId}/registrations/{registrationId}",
            new AdminRegisterAttendeeHttpResponse(registrationId));
    }
}
