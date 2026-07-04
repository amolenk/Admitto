using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.GetAttendeeEmails.AdminApi;

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
        IQueryHandler<GetAttendeeEmailsQuery, IReadOnlyList<AttendeeEmailLogItemDto>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetAttendeeEmailsQuery(
            TeamId: TeamId.From(teamId),
            EventId: TicketedEventId.From(eventId),
            RegistrationId: RegistrationId.From(registrationId));

        var result = await handler.HandleAsync(query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
