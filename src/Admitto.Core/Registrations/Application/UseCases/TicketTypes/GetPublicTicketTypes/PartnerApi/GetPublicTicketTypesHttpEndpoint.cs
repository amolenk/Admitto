using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.GetPublicTicketTypes.PartnerApi;

public static class GetPublicTicketTypesHttpEndpoint
{
    public static RouteGroupBuilder MapGetPublicTicketTypes(this RouteGroupBuilder group)
    {
        group.MapGet("/ticket-types", GetPublicTicketTypes)
            .WithName(nameof(GetPublicTicketTypes));

        return group;
    }

    private static async ValueTask<Ok<IReadOnlyList<PublicTicketTypeDto>>> GetPublicTicketTypes(
        HttpContext httpContext,
        Guid eventId,
        IQueryHandler<GetPublicTicketTypesQuery, IReadOnlyList<PublicTicketTypeDto>> handler,
        CancellationToken cancellationToken)
    {
        var teamId = httpContext.User.GetRequiredTeamId();
        var query = new GetPublicTicketTypesQuery(TicketedEventId.From(eventId), TeamId.From(teamId));

        var result = await handler.HandleAsync(query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
