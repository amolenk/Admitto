using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.GetEmailTemplates.AdminApi;

public static class GetEmailTemplatesHttpEndpoint
{
    public static RouteGroupBuilder MapGetEmailTemplates(
        this RouteGroupBuilder group,
        bool isEventScoped)
    {
        var endpointName = isEventScoped ? "GetEventEmailTemplates" : "GetTeamEmailTemplates";

        var handler = new Handler(isEventScoped);

        group
            .MapGet("/", handler.HandleAsync)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private sealed class Handler(bool isEventScoped)
    {
        public async ValueTask<Ok<IReadOnlyList<EmailTemplateListItemDto>>> HandleAsync(
            Guid teamId,
            Guid? eventId,
            IQueryHandler<GetEmailTemplatesQuery, IReadOnlyList<EmailTemplateListItemDto>> handler,
            CancellationToken ct)
        {
            var ticketedEventId = isEventScoped ? TicketedEventId.From(eventId!.Value) : (TicketedEventId?)null;

            var rows = await handler.HandleAsync(
                new GetEmailTemplatesQuery(
                    TeamId.From(teamId),
                    ticketedEventId), ct);

            return TypedResults.Ok(rows);
        }
    }
}
