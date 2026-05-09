using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.BulkEmails.GetBulkEmails.AdminApi;

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
