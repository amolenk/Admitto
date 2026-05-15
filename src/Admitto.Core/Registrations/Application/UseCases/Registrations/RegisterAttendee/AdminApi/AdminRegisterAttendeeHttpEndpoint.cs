using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendee.AdminApi;

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
        RegisterAttendeeHandler handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new RegisterAttendeeCommand(
            eventId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.TicketTypeIds,
            RegistrationMode.AdminAdd,
            CouponCode: null,
            EmailVerificationToken: null,
            AdditionalDetails: request.AdditionalDetails);

        var registrationId = await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/admin/teams/{teamId}/events/{eventId}/registrations/{registrationId}",
            new AdminRegisterAttendeeHttpResponse(registrationId));
    }
}
