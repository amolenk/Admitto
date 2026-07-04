using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.GetBulkEmails.AdminApi;

public static class GetBulkEmailsHttpEndpoint
{
    public static RouteGroupBuilder MapGetBulkEmails(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetBulkEmails)
            .WithName("GetBulkEmails")
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok<IReadOnlyList<BulkEmailListItemDto>>> GetBulkEmails(
        Guid teamId,
        Guid eventId,
        IQueryHandler<GetBulkEmailsQuery, IReadOnlyList<BulkEmailListItemDto>> handler,
        CancellationToken ct)
    {
        var rows = await handler.HandleAsync(
            new GetBulkEmailsQuery(TicketedEventId.From(eventId), TeamId.From(teamId)), ct);

        return TypedResults.Ok(rows);
    }
}
