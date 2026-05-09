using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.AttendeeEmails.GetAttendeeEmails.AdminApi;

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
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetAttendeeEmailsQuery(
            TeamId: teamId,
            EventId: eventId,
            RegistrationId: registrationId);

        var result = await mediator.QueryAsync<GetAttendeeEmailsQuery, IReadOnlyList<AttendeeEmailLogItemDto>>(
            query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
