using Amolenk.Admitto.Core.Email.Application.UseCases.AttendeeEmails.GetAttendeeEmails;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.AttendeeEmails.GetAttendeeEmails.AdminApi;

public static class GetAttendeeEmailsHttpEndpoint
{
    public static RouteGroupBuilder MapGetAttendeeEmails(this RouteGroupBuilder group)
    {
        group
            .MapGet("/emails", GetAttendeeEmails)
            .WithName(nameof(GetAttendeeEmails))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok<IReadOnlyList<AttendeeEmailLogItemDto>>> GetAttendeeEmails(
        Guid teamId,
        Guid eventId,
        Guid registrationId,
        GetAttendeeEmailsHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetAttendeeEmailsQuery(
            TeamId: teamId,
            EventId: eventId,
            RegistrationId: registrationId);

        var result = await handler.HandleAsync(query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
