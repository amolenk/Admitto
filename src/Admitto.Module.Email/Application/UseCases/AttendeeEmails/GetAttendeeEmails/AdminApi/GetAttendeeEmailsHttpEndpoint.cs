using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.AttendeeEmails.GetAttendeeEmails.AdminApi;

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
        string teamSlug,
        string eventSlug,
        Guid registrationId,
        IOrganizationScopeResolver scopeResolver,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, cancellationToken);

        var query = new GetAttendeeEmailsQuery(
            TeamId: scope.TeamId,
            EventId: scope.EventId!.Value,
            RegistrationId: registrationId);

        var result = await mediator.QueryAsync<GetAttendeeEmailsQuery, IReadOnlyList<AttendeeEmailLogItemDto>>(
            query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
