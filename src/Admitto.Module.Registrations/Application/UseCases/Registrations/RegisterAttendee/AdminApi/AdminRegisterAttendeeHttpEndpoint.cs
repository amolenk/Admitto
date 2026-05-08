using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterAttendee.AdminApi;

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
        string teamSlug,
        string eventSlug,
        IOrganizationScopeResolver scopeResolver,
        AdminRegisterAttendeeHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, cancellationToken);

        var command = new RegisterAttendeeCommand(
            scope.EventId!.Value,
            request.Email,
            request.FirstName,
            request.LastName,
            request.TicketTypeSlugs,
            RegistrationMode.AdminAdd,
            CouponCode: null,
            EmailVerificationToken: null,
            AdditionalDetails: request.AdditionalDetails);

        var registrationId = await mediator.SendReceiveAsync<RegisterAttendeeCommand, Guid>(
            command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/admin/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}",
            new AdminRegisterAttendeeHttpResponse(registrationId));
    }
}
