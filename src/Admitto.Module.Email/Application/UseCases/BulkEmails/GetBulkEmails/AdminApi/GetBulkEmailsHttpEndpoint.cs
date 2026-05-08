using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.GetBulkEmails.AdminApi;

public static class GetBulkEmailsHttpEndpoint
{
    public static RouteGroupBuilder MapGetBulkEmails(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", async (
                Guid teamId,
                Guid eventId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var rows = await mediator.QueryAsync<GetBulkEmailsQuery, IReadOnlyList<BulkEmailListItemDto>>(
                    new GetBulkEmailsQuery(TicketedEventId.From(eventId)), ct);

                return TypedResults.Ok(rows);
            })
            .WithName("GetBulkEmails")
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }
}
